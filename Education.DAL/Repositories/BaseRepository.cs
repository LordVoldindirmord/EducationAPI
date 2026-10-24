using Education.DAL.Context;
using Education.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly DbSet<T> _dbSet;
        protected readonly EducationDbContext _context;

        protected BaseRepository(EducationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task CreateAsync(T entity) =>
            await _dbSet.AddAsync(entity);

        public virtual void Delete(T entity) =>
            _dbSet.Remove(entity);

        public virtual async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet
                .AsNoTracking()
                .ToListAsync();

        public virtual async Task<T?> GetByIdAsync(Guid id) =>
            await _dbSet.FindAsync(id);

        public virtual async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public virtual void Update(T entity) =>
            _dbSet.Update(entity);
    }
}