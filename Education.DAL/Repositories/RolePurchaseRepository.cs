using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class RolePurchaseRepository : BaseRepository<RolePurchaseEntity>, IRolePurchaseRepository
    {
        public RolePurchaseRepository(EducationDbContext context) : base(context) { }
    }
}
