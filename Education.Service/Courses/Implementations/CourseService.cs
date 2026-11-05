using Education.Domain.DTO.Request.Courses.Courses;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Service.Common.Interfaces;
using Education.Service.Auth.Interfaces;
using Education.Service.Files.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Education.Service.Courses.Implementations
{
    // CheckAccessAsync - этот же метод мы будем реализовывать и в модулях с уроками
    // Пока что оставим так, но дальше лучше его вынести

    // Course.Price - лишняя пероверка на уровне сервисов. Можно вынести на уровень DTO и проверять атрибутами
    // Это внешний сервис, обращение к нему только через контроллер, поэтому это безопасно

    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IPermissionService _permissionService;
        private readonly ICourseAccessService _courseAccessService;
        private readonly IFileManager _fileManager;
        private readonly ILogger<CourseService> _logger;
        private readonly ICurrentUserService _currentUser;

        private static readonly UserRole[] CREATE_ROLES =
        {
            UserRole.Admin, UserRole.Moderator, UserRole.CourseCreator
        };

        public CourseService(
            ICourseRepository courseRepository,
            ICategoryRepository categoryRepository,
            IPermissionService permissionService,
            ICourseAccessService courseAccessService,
            IFileManager fileManager,
            ILogger<CourseService> logger,
            ICurrentUserService currentUser)
        {
            _courseRepository = courseRepository;
            _categoryRepository = categoryRepository;
            _permissionService = permissionService;
            _courseAccessService = courseAccessService;
            _fileManager = fileManager;
            _logger = logger;
            _currentUser = currentUser;
        }

        /// <inheritdoc />
        public async Task<InternalResponse<bool>> CreateCourseAsync(CreateCourseRequest request, Guid userId)
        {
            // Проверка пользователя и его прав
            if (!await _permissionService.IsActiveAsync(userId))
                return CreatorResponse.Forbidden<bool>("Аккаунт недоступен или заблокирован");

            if (!await _permissionService.HasAnyRoleAsync(userId, CREATE_ROLES))
                return CreatorResponse.Forbidden<bool>("Нет прав на создание курса");

            // Валидация входных параметров
            if (await _categoryRepository.GetByIdAsync(request.CategoryId) == null)
                return CreatorResponse.NotFound<bool>("Категория не найдена");

            if (await _courseRepository.ExistsByTitleAsync(request.CategoryId, request.Title))
                return CreatorResponse.Conflict<bool>("Курс с таким названием уже существует в категории");

            if (decimal.Round(request.Price, 2) != request.Price)
                return CreatorResponse.BadRequest<bool>("Цена должна содержать не более двух знаков после запятой");

            // Загрузка изображения
            var image = await UploadImageAsync(request.Image);

            if (!image.IsSuccess)
                return image.Repack<bool>();

            bool saved = false;

            try
            {
                var course = new CourseEntity
                {
                    Title = request.Title,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    Price = request.Price,
                    IsPublished = request.IsPublished,
                    ImagePath = image.Data!
                };

                // Курс и его владелец сохраняются атомарно
                course.Members.Add(
                    new CourseMemberEntity
                    {
                        UserId = userId,
                        Role = CourseRole.Owner
                    }
                );

                await _courseRepository.CreateAsync(course);
                await _courseRepository.SaveChangesAsync();

                saved = true;

                return CreatorResponse.Created(true, "Курс создан");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
            finally
            {
                // Если БД не сохранила курс, загруженная обложка больше не нужна
                if (!saved)
                    await DeleteFileAsync(image.Data!);
            }
        }

        /// <inheritdoc />
        public async Task<InternalResponse<bool>> DeleteCourseAsync(DeleteCourseRequest request, Guid userId)
        {
            // Проверяем права пользователя
            var access = await CheckAccessAsync(userId, request.CourseId, ownerOnly: true);

            if (access != null)
                return access;

            // Получаем курсы с его контентом
            var course = await _courseRepository.GetWithContentAsync(request.CourseId);

            if (course == null)
                return CreatorResponse.NotFound<bool>("Курс не найден");

            if (course.Purchases.Any(p => p.Status is PurchaseStatus.Completed or PurchaseStatus.Refunded))
                return CreatorResponse.Conflict<bool>("Нельзя удалить курс с успешными покупками в истории");

            if (course.Purchases.Any(p => p.Status != PurchaseStatus.Failed))
                return CreatorResponse.Conflict<bool>("Дождитесь завершения ожидающих платежей");

            // Собираем пути до удаления записей, иначе потеряем сведения о файлах уроков
            // Потом здесь нужно будет собирать сведения о всех файлах, которые встречаются в этом курсе
            var files = new List<string> { course.ImagePath };

            foreach (var lesson in course.Modules.SelectMany(m => m.Lessons))
            {
                if (lesson.VideoLesson != null)
                {
                    files.Add(lesson.VideoLesson.VideoPath);
                    files.Add(lesson.VideoLesson.ThumbnailPath);
                }
            }
            try
            {
                _courseRepository.DeleteWithContent(course);
                await _courseRepository.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }

            // Диск и БД не имеют общей транзакции: файлы удаляем только после БД
            // При сбое очистки сообщаем об этом и сохраняем пути в журнале
            bool cleaned = true;

            foreach (var path in files.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
                if (!await DeleteFileAsync(path)) cleaned = false;

            return CreatorResponse.Ok(true, cleaned ? "Курс и его содержимое удалены"
                : "Курс удален, но часть файлов удалить не удалось. Пути записаны в журнал");
        }

        /// <inheritdoc />
        public async Task<InternalResponse<bool>> PublishCourseAsync(PublishCourseRequest request, Guid userId)
        {
            // Проверка прав пользователя
            var access = await CheckAccessAsync(userId, request.CourseId, ownerOnly: true);

            if (access != null)
                return access;

            // Проверка корректности входных данных
            var course = await _courseRepository.GetByIdAsync(request.CourseId);

            if (course == null)
                return CreatorResponse.NotFound<bool>("Курс не найден");

            if (course.IsPublished == request.IsPublished)
                return CreatorResponse.Ok(true, "Статус публикации уже установлен");

            course.IsPublished = request.IsPublished;

            try
            {
                // Сохранение данных в БД
                await _courseRepository.SaveChangesAsync();

                return CreatorResponse.Ok(true, request.IsPublished ? "Курс опубликован" : "Курс снят с публикации");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
        }

        /// <inheritdoc />
        public async Task<InternalResponse<bool>> UpdateCourseAsync(UpdateCourseRequest request, Guid userId)
        {
            // Проверка прав пользователя
            var access = await CheckAccessAsync(userId, request.CourseId, ownerOnly: false);

            if (access != null)
                return access;

            var course = await _courseRepository.GetByIdAsync(request.CourseId);

            if (course == null)
                return CreatorResponse.NotFound<bool>("Курс не найден");

            if (await _categoryRepository.GetByIdAsync(request.NewCategoryId) == null)
                return CreatorResponse.NotFound<bool>("Категория не найдена");

            if (await _courseRepository.ExistsByTitleAsync(request.NewCategoryId, request.NewTitle, course.Id))
                return CreatorResponse.Conflict<bool>("Курс с таким названием уже существует в категории");

            if (decimal.Round(request.NewPrice, 2) != request.NewPrice)
                return CreatorResponse.BadRequest<bool>("Цена должна содержать не более двух знаков после запятой");

            string? newImagePath = null;

            if (request.NewImage != null)
            {
                var image = await UploadImageAsync(request.NewImage);

                if (!image.IsSuccess)
                    return image.Repack<bool>();

                newImagePath = image.Data;
            }

            var oldImagePath = course.ImagePath;
            bool saved = false;

            try
            {
                course.Title = request.NewTitle;
                course.Description = request.NewDescription;
                course.Price = request.NewPrice;
                course.CategoryId = request.NewCategoryId;

                if (newImagePath != null) 
                    course.ImagePath = newImagePath;

                await _courseRepository.SaveChangesAsync();

                saved = true;
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
            finally
            {
                if (!saved && newImagePath != null) await DeleteFileAsync(newImagePath);
            }

            // Старую обложку удаляем только после успешного обновления БД
            if (newImagePath != null && !await DeleteFileAsync(oldImagePath))
                return CreatorResponse.Ok(true, "Курс обновлен, но старую обложку удалить не удалось");

            return CreatorResponse.Ok(true, "Курс обновлен");
        }

        /// <summary>
        /// Проверить доступ пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <param name="ownerOnly">Только для владельца этого курса</param>
        /// <returns>Имеет ли пользователь нужные права или нет</returns>
        private async Task<InternalResponse<bool>?> CheckAccessAsync(Guid userId, Guid courseId, bool ownerOnly)
        {
            if (!await _permissionService.IsActiveAsync(userId))
                return CreatorResponse.Forbidden<bool>("Аккаунт недоступен или заблокирован");

            var access = await _courseAccessService.GetAccessTypeAsync(userId, courseId);

            if (ownerOnly && access != CourseAccessType.Owner)
                return CreatorResponse.Forbidden<bool>("Нет прав (требуется Owner)");

            if (!ownerOnly && access < CourseAccessType.Manager)
                return CreatorResponse.Forbidden<bool>("Нет прав (требуется Owner или Manager)");

            return null;
        }

        /// <summary>
        /// Загрузить изображение асинхронно
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <returns>Относительный путь до изображения</returns>
        private async Task<InternalResponse<string>> UploadImageAsync(IFormFile image)
        {
            if (image.Length == 0)
                return CreatorResponse.BadRequest<string>("Обложка пустая");

            var extension = Path
                .GetExtension(image.FileName)
                .ToLowerInvariant();

            if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return CreatorResponse.BadRequest<string>("Обложка должна быть JPG, JPEG, PNG или WEBP");

            using var stream = image.OpenReadStream();

            return await _fileManager.UploadAsync(stream, image.FileName);
        }

        /// <summary>
        /// Удалить файл с диска
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns>Флаг успешности операции</returns>
        private async Task<bool> DeleteFileAsync(string path)
        {
            try
            {
                var response = await _fileManager.DeleteAsync(path);

                if (response.IsSuccess) 
                    return true;

                _logger.LogWarning("Не удалось удалить файл {Path}: {Description}", path, response.Description);
            }
            catch (Exception exception)
            {
                // Ошибка очистки не должна скрывать результат уже выполненной операции БД
                _logger.LogWarning(exception, "Не удалось удалить файл {Path}", path);
            }

            return false;
        }

        /// <summary>
        /// Перевести в ответ на уровне базы данных
        /// </summary>
        /// <param name="exception">Исключение</param>
        /// <returns>Ответ сервера</returns>
        private InternalResponse<bool> DatabaseError(DbUpdateException exception)
        {
            // Предварительные проверки не исключают одновременные запросы к БД
            if (exception.InnerException is PostgresException postgres)
            {
                if (postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                    return CreatorResponse.Conflict<bool>("Курс с таким названием уже существует в категории");

                if (postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation)
                    return CreatorResponse.Conflict<bool>("Связанные данные курса изменились или препятствуют операции");
            }

            _logger.LogError(exception, "Ошибка сохранения курса");

            return CreatorResponse.InternalError<bool>("Не удалось сохранить изменения курса");
        }
    }
}
