using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class UserProgressRepository : BaseRepository<UserProgressEntity>, IUserProgressRepository
    {
        public UserProgressRepository(EducationDbContext context) : base(context) { }

        public Task<UserProgressEntity?> GetByUserAndLessonTrackedAsync(Guid userId, Guid lessonId) => _dbSet
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);
    }
}
