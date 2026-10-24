using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface IEnrollmentRepository : IBaseRepository<EnrollmentEntity>
    {
        /// <summary>
        /// Проверить, записан ли пользователь на курс
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns>true - пользователь записан на курс, false - нет</returns>
        Task<bool> ExistsAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Получить запись студента на курс
        /// AsNoTracking НЕ вызывается внутри метода.
        /// </summary>
        Task<EnrollmentEntity?> GetByUserAndCourseTrackedAsync(Guid userId, Guid courseId);
    }
}