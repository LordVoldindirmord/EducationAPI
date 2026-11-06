using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Progress;
using Education.Domain.DTO.Response.Courses.Progress;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Education.Service.Courses.Implementations
{
    public class ProgressService : IProgressService
    {
        private readonly IUserProgressRepository _userProgressRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ICourseAccessService _courseAccessService;
        private readonly ILogger<ProgressService> _logger;

        public ProgressService(IUserProgressRepository userProgressRepository, ILessonRepository lessonRepository,
            IEnrollmentService enrollmentService, ICourseAccessService courseAccessService, ILogger<ProgressService> logger)
        {
            _userProgressRepository = userProgressRepository;
            _lessonRepository = lessonRepository;
            _enrollmentService = enrollmentService;
            _courseAccessService = courseAccessService;
            _logger = logger;
        }

        public async Task<InternalResponse<bool>> CompleteLessonAsync(Guid userId, Guid lessonId)
        {
            var lesson = await _lessonRepository.GetByIdAsync(lessonId);
            if (lesson == null)
                return CreatorResponse.NotFound<bool>("Урок не найден");

            var access = await CheckAccessAsync(userId, lesson);
            if (access != null) return access;

            if (lesson.LessonType == LessonType.Quiz)
                return CreatorResponse.BadRequest<bool>("Для теста используйте SubmitQuizAsync");

            if (lesson.LessonType is not (LessonType.Video or LessonType.Text))
                return CreatorResponse.BadRequest<bool>("Неизвестный тип урока");

            if (await _userProgressRepository.GetByUserAndLessonTrackedAsync(userId, lessonId) != null)
                return CreatorResponse.Ok(true, "Уже пройден");

            var progress = new UserProgressEntity
            {
                UserId = userId,
                LessonId = lessonId,
                CompletedAt = DateTime.UtcNow,
                Score = null,
                IsPassed = true
            };

            try
            {
                await _userProgressRepository.CreateAsync(progress);
                await _userProgressRepository.SaveChangesAsync();
                return CreatorResponse.Ok(true, "Урок пройден");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
        }

        public async Task<InternalResponse<QuizResultResponse>> SubmitQuizAsync(Guid userId, SubmitQuizRequest request)
        {
            var lesson = await _lessonRepository.GetWithContentAsync(request.LessonId);
            if (lesson == null)
                return CreatorResponse.NotFound<QuizResultResponse>("Урок не найден");

            var access = await CheckAccessAsync(userId, lesson);
            if (access != null) return access.Repack<QuizResultResponse>();

            if (lesson is not QuizLessonEntity quiz)
                return CreatorResponse.BadRequest<QuizResultResponse>("Урок не является тестом");

            if (quiz.Questions.Count == 0)
                return CreatorResponse.Conflict<QuizResultResponse>("Тест не содержит вопросов");

            if (request.Answers == null || request.Answers.Count != quiz.Questions.Count)
                return CreatorResponse.BadRequest<QuizResultResponse>("Количество ответов должно совпадать с количеством вопросов");

            var byOrder = new Dictionary<int, UserAnswer>();
            foreach (var answer in request.Answers)
            {
                if (answer == null || !byOrder.TryAdd(answer.QuestionOrderNumber, answer))
                    return CreatorResponse.BadRequest<QuizResultResponse>("Каждому вопросу должен соответствовать один ответ");
            }

            int correctCount = 0;
            foreach (var question in quiz.Questions)
            {
                if (!byOrder.TryGetValue(question.OrderNumber, out var answer))
                    return CreatorResponse.BadRequest<QuizResultResponse>("Ответы должны соответствовать вопросам теста");

                if (answer.SelectedOptions == null
                    || answer.SelectedOptions.Any(i => i < 0 || i >= question.Options.Count))
                    return CreatorResponse.BadRequest<QuizResultResponse>("Выбраны недопустимые индексы вариантов ответа");

                var selected = answer.SelectedOptions.ToHashSet();
                if (selected.Count != answer.SelectedOptions.Count)
                    return CreatorResponse.BadRequest<QuizResultResponse>("Индексы выбранных вариантов не должны повторяться");

                if (selected.SetEquals(question.CorrectAnswers)) correctCount++;
            }

            var score = (int)((long)correctCount * 100 / quiz.Questions.Count);
            var isPassed = score >= quiz.PassingScore;
            var progress = await _userProgressRepository.GetByUserAndLessonTrackedAsync(userId, quiz.Id);
            bool creating = progress == null;
            progress ??= new UserProgressEntity { UserId = userId, LessonId = quiz.Id };
            progress.Score = score;
            progress.IsPassed = isPassed;
            progress.CompletedAt = DateTime.UtcNow;

            try
            {
                if (creating) await _userProgressRepository.CreateAsync(progress);
                await _userProgressRepository.SaveChangesAsync();
                return CreatorResponse.Ok(new QuizResultResponse
                {
                    Score = score,
                    IsPassed = isPassed,
                    PassingScore = quiz.PassingScore
                }, isPassed ? "Тест пройден" : "Проходной балл не набран");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception).Repack<QuizResultResponse>();
            }
        }

        private async Task<InternalResponse<bool>?> CheckAccessAsync(Guid userId, LessonEntity lesson)
        {
            if (lesson.Module == null)
                return CreatorResponse.NotFound<bool>("Модуль урока не найден");

            var courseId = lesson.Module.CourseId;
            if (await _enrollmentService.IsEnrolledAsync(userId, courseId)
                || await _courseAccessService.HasAccessAsync(userId, courseId))
                return null;

            return CreatorResponse.Forbidden<bool>("Нет доступа к прохождению курса");
        }

        private InternalResponse<bool> DatabaseError(DbUpdateException exception)
        {
            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                return CreatorResponse.Conflict<bool>("Прогресс по этому уроку уже сохранен другим запросом. Повторите запрос");

            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                return CreatorResponse.Conflict<bool>("Связанные данные прогресса изменились или были удалены");

            _logger.LogError(exception, "Ошибка сохранения прогресса");
            return CreatorResponse.InternalError<bool>("Не удалось сохранить прогресс");
        }
    }
}
