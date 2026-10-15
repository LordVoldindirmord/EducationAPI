using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class CategoryRepository : BaseRepository<CategoryEntity>, ICategoryRepository
    {
        public CategoryRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> ExistByName(string name)
        {
            return await _dbSet.AnyAsync(c => c.Name == name);
        }

        public async Task<bool> HasCoursesAsync(Guid categoryId)
        {
            return await _context.Courses
                .AnyAsync(c => c.CategoryId == categoryId);
        }
    }
}
