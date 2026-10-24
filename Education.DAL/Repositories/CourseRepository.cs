using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class CourseRepository : BaseRepository<CourseEntity>, ICourseRepository
    {
        public CourseRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> IsPublishedAsync(Guid courseId)
        {
            return await _dbSet.AnyAsync(c => c.Id == courseId && c.IsPublished);
        }
    }
}