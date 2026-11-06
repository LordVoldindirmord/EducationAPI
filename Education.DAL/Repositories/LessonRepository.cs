using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class LessonRepository : BaseRepository<LessonEntity>, ILessonRepository
    {
        public LessonRepository(EducationDbContext context) : base(context) { }

        public override Task<LessonEntity?> GetByIdAsync(Guid id) => _dbSet
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == id);

        public Task<LessonEntity?> GetWithContentAsync(Guid id) => _dbSet
            .Include(l => l.Module)
            .Include(l => l.Progresses)
            .Include(l => ((QuizLessonEntity)l).Questions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);

        public override void Delete(LessonEntity entity)
        {
            // Прогресс связан через Restrict, остальной контент удаляет каскад БД.
            _context.UserProgresses.RemoveRange(entity.Progresses);
            base.Delete(entity);
        }
    }
}
