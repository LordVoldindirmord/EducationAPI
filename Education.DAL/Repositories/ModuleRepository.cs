using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class ModuleRepository : BaseRepository<ModuleEntity>, IModuleRepository
    {
        public ModuleRepository(EducationDbContext context) : base(context) { }

        public Task<ModuleEntity?> GetWithContentAsync(Guid id) => _dbSet
            .Include(m => m.Lessons).ThenInclude(l => l.Progresses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);

        public override void Delete(ModuleEntity entity)
        {
            // Прогресс связан через Restrict, остальной контент удаляет каскад БД.
            _context.UserProgresses.RemoveRange(entity.Lessons.SelectMany(l => l.Progresses));
            base.Delete(entity);
        }
    }
}
