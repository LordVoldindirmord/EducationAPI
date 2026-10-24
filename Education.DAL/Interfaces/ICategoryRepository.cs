using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface ICategoryRepository : IBaseRepository<CategoryEntity>
    {
        /// <summary>
        /// Проверить существование категории по ее названию
        /// </summary>
        /// <param name="name">Название категории</param>
        /// <returns>true - категория существует, false - нет</returns>
        Task<bool> ExistByName(string name);

        /// <summary>
        /// Проверить, есть ли у категории курсы
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <returns>true - курсы есть, false - курсов нет</returns>
        Task<bool> HasCoursesAsync(Guid categoryId);
    }
}