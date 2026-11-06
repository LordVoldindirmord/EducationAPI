using System.ComponentModel.DataAnnotations;
using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Lessons;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;
using Education.Service.Files.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Education.Service.Courses.Implementations
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly ICourseAccessService _courseAccessService;
        private readonly IFileManager _fileManager;
        private readonly EducationDbContext _context;
        private readonly ILogger<LessonService> _logger;

        public LessonService(ILessonRepository lessonRepository, IModuleRepository moduleRepository,
            ICourseAccessService courseAccessService, IFileManager fileManager,
            EducationDbContext context, ILogger<LessonService> logger)
        {
            _lessonRepository = lessonRepository;
            _moduleRepository = moduleRepository;
            _courseAccessService = courseAccessService;
            _fileManager = fileManager;
            _context = context;
            _logger = logger;
        }

        public async Task<InternalResponse<bool>> CreateLessonAsync(CreateLessonRequest request, Guid userId)
        {
            var access = await CheckAccessAsync(userId, request.ModuleId);
            if (access != null) return access;

            var error = ValidateContent(request.LessonType, request.VideoFile, request.ThumbnailFile,
                request.DurationSeconds, request.TextContent, request.Questions, request.PassingScore,
                request.TimeLimitMinutes, request.ShowErrorsImmediately, creating: true);
            if (error != null) return CreatorResponse.BadRequest<bool>(error);

            var uploaded = new List<string>();
            bool saved = false;
            try
            {
                LessonEntity lesson;
                switch (request.LessonType)
                {
                    case LessonType.Video:
                        var video = await UploadFileAsync(request.VideoFile!, video: true);
                        if (!video.IsSuccess) return video.Repack<bool>();
                        uploaded.Add(video.Data!);

                        var thumbnail = await UploadFileAsync(request.ThumbnailFile!, video: false);
                        if (!thumbnail.IsSuccess) return thumbnail.Repack<bool>();
                        uploaded.Add(thumbnail.Data!);

                        lesson = new VideoLessonEntity
                        {
                            LessonType = LessonType.Video, OrderNumber = request.OrderNumber,
                            VideoPath = video.Data!, ThumbnailPath = thumbnail.Data!,
                            DurationSeconds = request.DurationSeconds!.Value
                        };
                        break;
                    case LessonType.Text:
                        lesson = new TextLessonEntity
                        {
                            LessonType = LessonType.Text, OrderNumber = request.OrderNumber,
                            Content = request.TextContent!
                        };
                        break;
                    case LessonType.Quiz:
                        lesson = new QuizLessonEntity
                        {
                            LessonType = LessonType.Quiz, OrderNumber = request.OrderNumber,
                            PassingScore = request.PassingScore, TimeLimitMinutes = request.TimeLimitMinutes,
                            ShowErrorsImmediately = request.ShowErrorsImmediately,
                            Questions = request.Questions!.Select(CreateQuestion).ToList()
                        };
                        break;
                    default:
                        return CreatorResponse.BadRequest<bool>("Неизвестный тип урока");
                }

                lesson.ModuleId = request.ModuleId;
                lesson.Title = request.Title;
                lesson.IsPublished = request.IsPublished;

                // Базовая запись, TPT-контент и вопросы сохраняются в общей транзакции.
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _lessonRepository.CreateAsync(lesson);
                await _lessonRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                saved = true;
                return CreatorResponse.Created(true, "Урок создан");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
            finally
            {
                // Dispose транзакции откатывает БД; очистка нужна и при сбое второй загрузки.
                if (!saved) await DeleteFilesAsync(uploaded);
            }
        }

        public async Task<InternalResponse<bool>> UpdateLessonAsync(UpdateLessonRequest request, Guid userId)
        {
            var lesson = await _lessonRepository.GetByIdAsync(request.LessonId);
            if (lesson == null) return CreatorResponse.NotFound<bool>("Урок не найден");

            var access = await CheckAccessAsync(userId, lesson.ModuleId);
            if (access != null) return access;

            lesson = await _lessonRepository.GetWithContentAsync(request.LessonId);
            if (lesson == null) return CreatorResponse.NotFound<bool>("Урок не найден");

            var error = ValidateContent(lesson.LessonType, request.NewVideoFile, request.NewThumbnailFile,
                request.NewDurationSeconds, request.NewTextContent, request.NewQuestions,
                request.NewPassingScore, request.NewTimeLimitMinutes, request.NewShowErrorsImmediately, creating: false);
            if (error != null) return CreatorResponse.BadRequest<bool>(error);

            if (request.RemoveTimeLimit && (lesson is not QuizLessonEntity || request.NewTimeLimitMinutes != null))
                return CreatorResponse.BadRequest<bool>("Снятие лимита времени допустимо только для теста без нового лимита");

            var uploaded = new List<string>();
            var obsolete = new List<string>();
            bool saved = false;
            try
            {
                switch (lesson)
                {
                    case VideoLessonEntity video:
                        if (request.NewVideoFile != null)
                        {
                            var file = await UploadFileAsync(request.NewVideoFile, video: true);
                            if (!file.IsSuccess) return file.Repack<bool>();
                            uploaded.Add(file.Data!);
                            obsolete.Add(video.VideoPath);
                            video.VideoPath = file.Data!;
                        }
                        if (request.NewThumbnailFile != null)
                        {
                            var file = await UploadFileAsync(request.NewThumbnailFile, video: false);
                            if (!file.IsSuccess) return file.Repack<bool>();
                            uploaded.Add(file.Data!);
                            obsolete.Add(video.ThumbnailPath);
                            video.ThumbnailPath = file.Data!;
                        }
                        if (request.NewDurationSeconds.HasValue) video.DurationSeconds = request.NewDurationSeconds.Value;
                        break;
                    case TextLessonEntity text:
                        if (request.NewTextContent != null) text.Content = request.NewTextContent;
                        break;
                    case QuizLessonEntity quiz:
                        if (request.NewPassingScore.HasValue) quiz.PassingScore = request.NewPassingScore.Value;
                        if (request.RemoveTimeLimit) quiz.TimeLimitMinutes = null;
                        else if (request.NewTimeLimitMinutes.HasValue) quiz.TimeLimitMinutes = request.NewTimeLimitMinutes;
                        if (request.NewShowErrorsImmediately.HasValue) quiz.ShowErrorsImmediately = request.NewShowErrorsImmediately.Value;
                        if (request.NewQuestions != null) UpdateQuestions(quiz, request.NewQuestions);
                        break;
                    default:
                        return CreatorResponse.InternalError<bool>("Неизвестный тип контента урока");
                }

                lesson.Title = request.NewTitle;
                lesson.OrderNumber = request.NewOrderNumber;
                lesson.IsPublished = request.IsPublished;
                await _lessonRepository.SaveChangesAsync();
                saved = true;
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
            finally
            {
                if (!saved) await DeleteFilesAsync(uploaded);
            }

            var cleaned = await DeleteFilesAsync(obsolete.Except(uploaded));
            return CreatorResponse.Ok(true, cleaned ? "Урок обновлен"
                : "Урок обновлен, но старые файлы удалить не удалось. Пути записаны в журнал");
        }

        public async Task<InternalResponse<bool>> DeleteLessonAsync(DeleteLessonRequest request, Guid userId)
        {
            var lesson = await _lessonRepository.GetByIdAsync(request.LessonId);
            if (lesson == null) return CreatorResponse.NotFound<bool>("Урок не найден");

            var access = await CheckAccessAsync(userId, lesson.ModuleId);
            if (access != null) return access;

            lesson = await _lessonRepository.GetWithContentAsync(request.LessonId);
            if (lesson == null) return CreatorResponse.NotFound<bool>("Урок не найден");

            var paths = lesson is VideoLessonEntity video
                ? new[] { video.VideoPath, video.ThumbnailPath } : Array.Empty<string>();
            try
            {
                _lessonRepository.Delete(lesson);
                await _lessonRepository.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }

            var cleaned = await DeleteFilesAsync(paths);
            return CreatorResponse.Ok(true, cleaned ? "Урок удален"
                : "Урок удален, но часть файлов удалить не удалось. Пути записаны в журнал");
        }

        private async Task<InternalResponse<bool>?> CheckAccessAsync(Guid userId, Guid moduleId)
        {
            var module = await _moduleRepository.GetByIdAsync(moduleId);
            if (module == null) return CreatorResponse.NotFound<bool>("Модуль не найден");
            if (!await _courseAccessService.CanManageAsync(userId, module.CourseId))
                return CreatorResponse.Forbidden<bool>("Нет прав на управление курсом");
            return null;
        }

        private static string? ValidateContent(LessonType type, IFormFile? video, IFormFile? thumbnail,
            int? duration, string? text, List<TestQuestionRequest>? questions,
            int? passingScore, int? timeLimit, bool? showErrors, bool creating)
        {
            if (!Enum.IsDefined(type)) return "Неизвестный тип урока";
            if (type != LessonType.Video && (video != null || thumbnail != null || duration != null))
                return "Видеоконтент допустим только для видеоурока";
            if (type != LessonType.Text && text != null) return "Текстовый контент допустим только для текстового урока";
            if (type != LessonType.Quiz && (questions != null || timeLimit != null
                || (creating ? passingScore != 0 || showErrors == true : passingScore != null || showErrors != null)))
                return "Параметры теста допустимы только для тестового урока";

            if (type == LessonType.Video)
            {
                if (creating && (video == null || thumbnail == null || duration == null))
                    return "Для видеоурока обязательны видео, превью и длительность";
                if (duration <= 0) return "Длительность должна быть больше нуля";
            }
            if (type == LessonType.Text && (creating || text != null)
                && (string.IsNullOrWhiteSpace(text) || text.Length > 32192))
                return "Текст урока обязателен и не должен превышать 32192 символа";

            if (type == LessonType.Quiz)
            {
                if (passingScore is < 0 or > 100 || timeLimit <= 0) return "Некорректные параметры теста";
                if (creating && questions == null || questions is { Count: 0 }) return "Добавьте вопросы теста";
                if (questions != null)
                {
                    if (questions.Select(q => q?.OrderNumber).Distinct().Count() != questions.Count)
                        return "Порядковые номера вопросов должны быть уникальны";
                    foreach (var question in questions)
                    {
                        if (question == null) return "Вопрос не должен быть пустым";
                        var results = new List<ValidationResult>();
                        if (!Validator.TryValidateObject(question, new ValidationContext(question), results, true))
                            return string.Join("; ", results.Select(r => r.ErrorMessage));
                    }
                }
            }
            return null;
        }

        private static TestQuestionEntity CreateQuestion(TestQuestionRequest request) => new()
        {
            QuestionText = request.QuestionText, Options = request.Options.ToList(),
            CorrectAnswers = request.CorrectAnswers.ToList(), OrderNumber = request.OrderNumber,
            Explanation = request.Explanation
        };

        private static void UpdateQuestions(QuizLessonEntity quiz, List<TestQuestionRequest> requests)
        {
            // Обновляем по номеру, чтобы не создавать конфликт уникального индекса при замене вопросов.
            var byOrder = requests.ToDictionary(q => q.OrderNumber);
            foreach (var existing in quiz.Questions.ToList())
            {
                if (!byOrder.Remove(existing.OrderNumber, out var request))
                {
                    quiz.Questions.Remove(existing);
                    continue;
                }
                existing.QuestionText = request.QuestionText;
                existing.Options = request.Options.ToList();
                existing.CorrectAnswers = request.CorrectAnswers.ToList();
                existing.Explanation = request.Explanation;
            }
            foreach (var request in byOrder.Values) quiz.Questions.Add(CreateQuestion(request));
        }

        private async Task<InternalResponse<string>> UploadFileAsync(IFormFile file, bool video)
        {
            if (file.Length == 0) return CreatorResponse.BadRequest<string>("Файл пустой");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (video ? extension != ".mp4" : extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return CreatorResponse.BadRequest<string>(video ? "Видео должно быть MP4" : "Превью должно быть JPG, JPEG, PNG или WEBP");
            using var stream = file.OpenReadStream();
            return await _fileManager.UploadAsync(stream, file.FileName);
        }

        private async Task<bool> DeleteFilesAsync(IEnumerable<string> paths)
        {
            bool cleaned = true;
            foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
            {
                try
                {
                    var response = await _fileManager.DeleteAsync(path);
                    if (response.IsSuccess) continue;
                    _logger.LogWarning("Не удалось удалить файл {Path}: {Description}", path, response.Description);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Не удалось удалить файл {Path}", path);
                }
                cleaned = false;
            }
            return cleaned;
        }

        private InternalResponse<bool> DatabaseError(DbUpdateException exception)
        {
            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                return CreatorResponse.Conflict<bool>("Урок или вопрос с таким порядковым номером уже существует");
            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                return CreatorResponse.Conflict<bool>("Связанные данные урока изменились или препятствуют операции");
            _logger.LogError(exception, "Ошибка сохранения урока");
            return CreatorResponse.InternalError<bool>("Не удалось сохранить изменения урока");
        }
    }
}
