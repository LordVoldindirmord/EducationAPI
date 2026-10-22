using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class PurchaseRepository : BaseRepository<PurchaseEntity>, IPurchaseRepository
    {
        public PurchaseRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> ExistsCompletedCourseAsync(Guid userId, Guid courseId)
        {
            return await _dbSet
                .OfType<CoursePurchaseEntity>()
                .AnyAsync(
                    p => p.UserId == userId
                    && p.CourseId == courseId
                    && p.Status == PurchaseStatus.Completed
                );
        }

        public async Task<bool> ExistsCompletedRoleAsync(Guid userId, Guid roleId)
        {
            return await _dbSet
                .OfType<RolePurchaseEntity>()
                .AnyAsync(
                    p => p.UserId == userId
                    && p.RoleId == roleId
                    && p.Status == PurchaseStatus.Completed
                );
        }

        public async Task<PurchaseEntity?> GetByTransactionIdTrackedAsync(string transactionId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }
    }
}
