using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    /// <summary>
    /// Репозиторий уроков курса
    /// </summary>
    public interface ILessonRepository : IBaseRepository<LessonEntity>
    {
        /// <summary>
        /// Получить урок с модулем, контентом, вопросами теста и прогрессом пользователей с отслеживанием изменений.
        /// Данные используются для удаления содержимого и обновления урока.
        /// </summary>
        /// <param name="id">Id урока</param>
        /// <returns>Сущность, если урок по Id найден, null, если нет</returns>
        Task<LessonEntity?> GetWithContentAsync(Guid id);

    }
}
