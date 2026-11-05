using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface ICourseRepository : IBaseRepository<CourseEntity>
    {
        /// <summary>
        /// Проверить, опубликован ли курс
        /// </summary>
        /// <param name="courseId">Id курса</param>
        /// <returns>true - курс опубликован, false - нет</returns>
        Task<bool> IsPublishedAsync(Guid courseId);

        /// <summary>
        /// Проверить уникальность названия внутри категории, исключая указанный курс
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <param name="title">Название курса</param>
        /// <param name="excludedCourseId">Id курса, который нужно исключить из проверки</param>
        /// <returns>true - название уникально, false - нет</returns>
        Task<bool> ExistsByTitleAsync(Guid categoryId, string title, Guid? excludedCourseId = null);

        /// <summary>
        /// Получить курс с данными, необходимыми для удаления содержимого
        /// </summary>
        /// <param name="courseId">Id курса</param>
        /// <returns>Сущность, если курс по Id найден, null, если нет</returns>
        Task<CourseEntity?> GetWithContentAsync(Guid courseId);

        /// <summary>
        /// Подготовить удаление курса и зависимых данных
        /// Вызывающий сервис проверяет покупки
        /// Изменения фиксируются отдельным вызовом SaveChangesAsync
        /// </summary>
        void DeleteWithContent(CourseEntity course);
    }
}
