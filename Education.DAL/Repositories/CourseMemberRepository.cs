using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class CourseMemberRepository : BaseRepository<CourseMemberEntity>, ICourseMemberRepository
    {
        public CourseMemberRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> ExistsAsync(Guid userId, Guid courseId)
        {
            return await _dbSet
                .AnyAsync(cm => cm.UserId == userId && cm.CourseId == courseId);
        }

        public async Task<CourseMemberEntity?> GetByUserAndCourseAsync(Guid userId, Guid courseId)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(cm => cm.UserId == userId && cm.CourseId == courseId);
        }
    }
}