using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    /// <summary>
    /// Репозиторий модулей курса
    /// </summary>
    public interface IModuleRepository : IBaseRepository<ModuleEntity>
    {
        /// <summary>
        /// Получить модуль с уроками, их контентом и прогрессом пользователей с отслеживанием изменений.
        /// Данные используются для удаления содержимого.
        /// </summary>
        /// <param name="id">Id модуля</param>
        /// <returns>Сущность, если модуль по Id найден, null, если нет</returns>
        Task<ModuleEntity?> GetWithContentAsync(Guid id);

    }
}
