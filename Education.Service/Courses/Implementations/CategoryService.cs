using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Categories;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.ResponseModel;
using Education.Service.Common.Interfaces;
using Education.Service.Courses.Interfaces;

namespace Education.Service.Courses.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IPermissionService _permissionService;

        /// <summary>
        /// Роли пользователей, которые могут обращаться к данному классу
        /// </summary>
        private static readonly UserRole[] RolesPermission =
        {
            UserRole.Admin,
            UserRole.Moderator,
        };

        public CategoryService(
            ICategoryRepository categoryRepository,
            IPermissionService permissionService)
        {
            _categoryRepository = categoryRepository;
            _permissionService = permissionService;
        }

        public async Task<InternalResponse<bool>> CreateCategoryAsync(CreateCategoryRequest request, Guid userId)
        {
            // Проверка прав
            var checkPermission = await CheckAccessAsync<bool>(userId);

            if (checkPermission != null)
                return checkPermission;

            // Проверка, что такого названия еще нет
            if (await _categoryRepository.ExistByName(request.Name))
                return CreatorResponse.Conflict<bool>("Категория с таким названием уже существует");

            // Создание категории
            CategoryEntity category = new CategoryEntity
            {
                Name = request.Name,
                Description = request.Description,
            };

            await _categoryRepository.CreateAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return CreatorResponse.Created(true, "Категория успешно создана");
        }

        public async Task<InternalResponse<bool>> DeleteCategoryAsync(DeleteCategoryRequest request, Guid userId)
        {
            // Проверка прав
            var checkPermission = await CheckAccessAsync<bool>(userId);

            if (checkPermission != null)
                return checkPermission;

            // Получение самой категории
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);

            if (category == null)
                return CreatorResponse.NotFound<bool>("Не найдена категория для удаления");

            // Проверка, есть ли у категории курсы
            if (await _categoryRepository.HasCoursesAsync(request.CategoryId))
                return CreatorResponse.Conflict<bool>("Категория не может быть удалена, так как в ней уже есть курсы");

            // Удаление категории
            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();

            return CreatorResponse.Ok(true, "Категория успешно удалена");
        }

        public async Task<InternalResponse<bool>> UpdateCategoryAsync(UpdateCategoryRequest request, Guid userId)
        {
            // Проверка прав
            var checkPermission = await CheckAccessAsync<bool>(userId);

            if (checkPermission != null)
                return checkPermission;

            // Получение самой категории
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);

            if (category == null)
                return CreatorResponse.NotFound<bool>("Не найдена категория для обновления");

            // Проверка, что что-то реально изменилось
            if (category.Name == request.NewName && category.Description == request.NewDescription)
                return CreatorResponse.Ok(true, "Нет изменений для обновления");

            // Проверка имени — только если оно изменилось
            if (category.Name != request.NewName
                && await _categoryRepository.ExistByName(request.NewName))
                return CreatorResponse.Conflict<bool>("Категория с таким названием уже существует");

            // Обновление категории
            category.Name = request.NewName;
            category.Description = request.NewDescription;
                        
            await _categoryRepository.SaveChangesAsync();

            return CreatorResponse.Ok(true, "Категория успешно обновлена");
        }

        /// <summary>
        /// Проверить, что пользователь активен и имеет доступ к операциям сервиса.
        /// Возвращает null, если доступ есть. Иначе — готовый ответ с ошибкой.
        /// </summary>
        private async Task<InternalResponse<T>?> CheckAccessAsync<T>(Guid userId)
        {
            if (!await _permissionService.IsActiveAsync(userId))
                return CreatorResponse.Forbidden<T>("Аккаунт заблокирован");

            if (!await _permissionService.HasAnyRoleAsync(userId, RolesPermission))
                return CreatorResponse.Forbidden<T>("Нет прав для данной операции");

            return null;
        }
    }
}