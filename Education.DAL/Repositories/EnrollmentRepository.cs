using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class EnrollmentRepository : BaseRepository<EnrollmentEntity>, IEnrollmentRepository
    {
        public EnrollmentRepository(EducationDbContext context) : base(context) { }
    }
}
