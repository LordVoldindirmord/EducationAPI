using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Modules;
using Education.Domain.Entity;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;
using Education.Service.Files.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Education.Service.Courses.Implementations
{
    public class ModuleService : IModuleService
    {
        private readonly IModuleRepository _moduleRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly ICourseAccessService _courseAccessService;
        private readonly IFileManager _fileManager;
        private readonly ILogger<ModuleService> _logger;

        public ModuleService(IModuleRepository moduleRepository, ICourseRepository courseRepository,
            ICourseAccessService courseAccessService, IFileManager fileManager, ILogger<ModuleService> logger)
        {
            _moduleRepository = moduleRepository;
            _courseRepository = courseRepository;
            _courseAccessService = courseAccessService;
            _fileManager = fileManager;
            _logger = logger;
        }

        public async Task<InternalResponse<bool>> CreateModuleAsync(CreateModuleRequest request, Guid userId)
        {
            if (await _courseRepository.GetByIdAsync(request.CourseId) == null)
                return CreatorResponse.NotFound<bool>("Курс не найден");

            if (!await _courseAccessService.CanManageAsync(userId, request.CourseId))
                return CreatorResponse.Forbidden<bool>("Нет прав на управление курсом");

            var module = new ModuleEntity
            {
                CourseId = request.CourseId,
                Title = request.Title,
                Description = request.Description,
                OrderNumber = request.OrderNumber
            };

            try
            {
                await _moduleRepository.CreateAsync(module);
                await _moduleRepository.SaveChangesAsync();
                return CreatorResponse.Created(true, "Модуль создан");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
        }

        public async Task<InternalResponse<bool>> UpdateModuleAsync(UpdateModuleRequest request, Guid userId)
        {
            var module = await _moduleRepository.GetByIdAsync(request.ModuleId);
            if (module == null)
                return CreatorResponse.NotFound<bool>("Модуль не найден");

            if (!await _courseAccessService.CanManageAsync(userId, module.CourseId))
                return CreatorResponse.Forbidden<bool>("Нет прав на управление курсом");

            module.Title = request.NewTitle;
            module.Description = request.NewDescription;
            module.OrderNumber = request.NewOrderNumber;

            try
            {
                await _moduleRepository.SaveChangesAsync();
                return CreatorResponse.Ok(true, "Модуль обновлен");
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }
        }

        public async Task<InternalResponse<bool>> DeleteModuleAsync(DeleteModuleRequest request, Guid userId)
        {
            var module = await _moduleRepository.GetByIdAsync(request.ModuleId);
            if (module == null)
                return CreatorResponse.NotFound<bool>("Модуль не найден");

            if (!await _courseAccessService.CanManageAsync(userId, module.CourseId))
                return CreatorResponse.Forbidden<bool>("Нет прав на управление курсом");

            module = await _moduleRepository.GetWithContentAsync(request.ModuleId);
            if (module == null)
                return CreatorResponse.NotFound<bool>("Модуль не найден");

            var paths = module.Lessons.OfType<VideoLessonEntity>()
                .SelectMany(v => new[] { v.VideoPath, v.ThumbnailPath })
                .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

            try
            {
                _moduleRepository.Delete(module);
                await _moduleRepository.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                return DatabaseError(exception);
            }

            bool cleaned = true;
            foreach (var path in paths)
                if (!await DeleteFileAsync(path)) cleaned = false;

            return CreatorResponse.Ok(true, cleaned ? "Модуль и его содержимое удалены"
                : "Модуль удален, но часть файлов удалить не удалось. Пути записаны в журнал");
        }

        private async Task<bool> DeleteFileAsync(string path)
        {
            try
            {
                var response = await _fileManager.DeleteAsync(path);
                if (response.IsSuccess) return true;
                _logger.LogWarning("Не удалось удалить файл {Path}: {Description}", path, response.Description);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Не удалось удалить файл {Path}", path);
            }
            return false;
        }

        private InternalResponse<bool> DatabaseError(DbUpdateException exception)
        {
            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                return CreatorResponse.Conflict<bool>("Модуль с таким порядковым номером уже существует в курсе");

            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                return CreatorResponse.Conflict<bool>("Связанные данные модуля изменились или препятствуют операции");

            _logger.LogError(exception, "Ошибка сохранения модуля");
            return CreatorResponse.InternalError<bool>("Не удалось сохранить изменения модуля");
        }
    }
}
