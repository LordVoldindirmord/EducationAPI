using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Repositories
{
    public class RolePurchaseRepository : BaseRepository<RolePurchaseEntity>, IRolePurchaseRepository
    {
        public RolePurchaseRepository(EducationDbContext context) : base(context) { }

        public async Task<bool> ExistsCompletedAsync(Guid userId, Guid roleId)
        {
            return await _dbSet.AnyAsync(
                p => p.UserId == userId
                && p.RoleId == roleId
                && p.Status == PurchaseStatus.Completed
            );
        }

        public async Task<RolePurchaseEntity?> GetByTransactionIdTrackedAsync(string transactionId)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }
    }
}
