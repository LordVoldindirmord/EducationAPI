using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class CourseMemberRepository : BaseRepository<CourseMemberEntity>, ICourseMemberRepository
    {
        public CourseMemberRepository(EducationDbContext context) : base(context) { }
    }
}
