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
    }
}
