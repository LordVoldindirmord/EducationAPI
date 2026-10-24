using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class EnrollmentRepository : BaseRepository<EnrollmentEntity>, IEnrollmentRepository
    {
        public EnrollmentRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> ExistsAsync(Guid userId, Guid courseId)
        {
            return await _dbSet.AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
        }

        public async Task<EnrollmentEntity?> GetByUserAndCourseTrackedAsync(Guid userId, Guid courseId)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        }
    }
}