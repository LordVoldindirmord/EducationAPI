using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class CoursePurchaseRepository : BaseRepository<CoursePurchaseEntity>, ICoursePurchaseRepository
    {
        public CoursePurchaseRepository(EducationDbContext context) : base(context) { }
    }
}
