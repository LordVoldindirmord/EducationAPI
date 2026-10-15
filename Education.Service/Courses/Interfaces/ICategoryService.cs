using Education.Domain.DTO.Request.Courses.Category;
using Education.Domain.ResponseModel;

namespace Education.Service.Courses.Interfaces
{
    public interface ICategoryService
    {
        /// <summary>
        /// Создать новую категорию
        /// </summary>
        /// <param name="request">DTO для создания категории</param>
        /// <param name="userId">Id пользователя, который хочет создать категорию</param>
        /// <returns>Флаг успешности операции (true - категория успешно создана, false - категория не создана)</returns>
        Task<InternalResponse<bool>> CreateCategoryAsync(CreateCategoryRequest request, Guid userId);

        /// <summary>
        /// Удалить категорию. Категория не удалится, если в ней уже есть курсы
        /// </summary>
        /// <param name="request">DTO для удаления категории</param>
        /// <param name="userId">Id пользователя, который хочет удалить категорию</param>
        /// <returns>Флаг успешности операции (true - категория успешно удалена, false - категория не удалена)</returns>
        Task<InternalResponse<bool>> DeleteCategoryAsync(DeleteCategoryRequest request, Guid userId);

        /// <summary>
        /// Переименовать категорию
        /// </summary>
        /// <param name="request">DTO для обновления категории</param>
        /// <param name="userId">Id пользователя, который хочет обновить категорию</param>
        /// <returns>Флаг успешности операции (true - категория успешно обновлена, false - категория не обновлена)</returns>
        Task<InternalResponse<bool>> UpdateCategoryAsync(UpdateCategoryRequest request, Guid userId);
    }
}
