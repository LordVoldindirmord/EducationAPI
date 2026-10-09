using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class CategoryRepository : BaseRepository<CategoryEntity>, ICategoryRepository
    {
        public CategoryRepository(EducationDbContext context) : base(context) { }        
    }
}
