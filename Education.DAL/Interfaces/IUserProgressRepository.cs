using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    /// <summary>
    /// Репозиторий прогресса пользователей по урокам
    /// </summary>
    public interface IUserProgressRepository : IBaseRepository<UserProgressEntity>
    {
        /// <summary>
        /// Получить прогресс пользователя по уроку с отслеживанием изменений.
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="lessonId">Id урока</param>
        /// <returns>Сущность прогресса, если запись найдена, null, если нет</returns>
        Task<UserProgressEntity?> GetByUserAndLessonTrackedAsync(Guid userId, Guid lessonId);
    }
}
