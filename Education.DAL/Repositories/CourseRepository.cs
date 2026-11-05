using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class CourseRepository : BaseRepository<CourseEntity>, ICourseRepository
    {
        public CourseRepository(EducationDbContext context) : base(context) { }

        public Task<CourseEntity?> GetWithContentAsync(Guid courseId)
        {
            return _dbSet
                .Include(c => c.Enrollments)
                .Include(c => c.Purchases)
                .Include(c => c.Modules).ThenInclude(m => m.Lessons).ThenInclude(l => l.Progresses)
                .Include(c => c.Modules).ThenInclude(m => m.Lessons).ThenInclude(l => l.VideoLesson)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public void DeleteWithContent(CourseEntity course)
        {
            // Эти связи имеют Restrict. Остальные дочерние сущности удаляет каскад БД.
            _context.UserProgresses.RemoveRange(course.Modules.SelectMany(m => m.Lessons).SelectMany(l => l.Progresses));
            _context.Enrollments.RemoveRange(course.Enrollments);
            _context.Purchase.RemoveRange(course.Purchases);
            _dbSet.Remove(course);
        }

        public Task<bool> ExistsByTitleAsync(Guid categoryId, string title, Guid? excludedCourseId = null)
        {
            return _dbSet.AnyAsync(c => c.CategoryId == categoryId && c.Title == title
                && (!excludedCourseId.HasValue || c.Id != excludedCourseId.Value));
        }

        public async Task<bool> IsPublishedAsync(Guid courseId)
        {
            return await _dbSet.AnyAsync(c => c.Id == courseId && c.IsPublished);
        }
    }
}
