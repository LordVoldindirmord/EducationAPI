using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface ICourseMemberRepository : IBaseRepository<CourseMemberEntity>
    {
        /// <summary>
        /// Получить запись участника курса по пользователю и курсу.
        /// AsNoTracking вызывается внутри метода.
        /// </summary>
        Task<CourseMemberEntity?> GetByUserAndCourseAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, является ли пользователь участником курса.
        /// </summary>
        Task<bool> ExistsAsync(Guid userId, Guid courseId);
    }
}
