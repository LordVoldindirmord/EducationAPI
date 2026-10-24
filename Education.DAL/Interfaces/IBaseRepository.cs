namespace Education.DAL.Interfaces
{
    /// <summary>
    /// Базовый интерфейс для всех репозиториев
    /// </summary>
    /// <typeparam name="T">Класс модели, с которой работает конкретный репозиторий</typeparam>
    public interface IBaseRepository<T>
    {
        /// <summary>
        /// Добавить сущность в БД
        /// </summary>
        /// <param name="entity">Сущность для добавления в БД</param>
        Task CreateAsync(T entity);

        /// <summary>
        /// Удалить сущность из БД
        /// </summary>
        /// <param name="entity">Сущность для удаления</param>
        void Delete(T entity);

        /// <summary>
        /// <para>Получить все сущности из таблицы в БД</para>
        /// <para>AsNoTracking вызывается внутри метода</para>
        /// </summary>
        /// <returns>Список сущностей из БД</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// <para>Получить сущность по Id</para>
        /// <para>AsNoTracking <b>НЕ</b> вызывается внутри метода</para>
        /// </summary>
        /// <param name="id">Id сущности</param>
        /// <returns>Сущность</returns>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Обновить сущность в БД
        /// </summary>
        /// <param name="entity">Обновленная сущность</param>
        void Update(T entity);

        /// <summary>
        /// Сохранить изменения в БД
        /// </summary>
        Task SaveChangesAsync();
    }
}