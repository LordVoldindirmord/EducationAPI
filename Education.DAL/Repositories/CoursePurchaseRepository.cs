using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class CoursePurchaseRepository : BaseRepository<CoursePurchaseEntity>, ICoursePurchaseRepository
    {
        public CoursePurchaseRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> ExistsCompletedAsync(Guid userId, Guid courseId)
        {
            return await _dbSet.AnyAsync(
                p => p.UserId == userId &&
                p.CourseId == courseId &&
                p.Status == PurchaseStatus.Completed
            );
        }

        public async Task<CoursePurchaseEntity?> GetByTransactionIdTrackedAsync(string transactionId)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }
    }
}
