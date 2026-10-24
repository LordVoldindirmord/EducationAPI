using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class UserProgressRepository : BaseRepository<UserProgressEntity>, IUserProgressRepository
    {
        public UserProgressRepository(EducationDbContext context) : base(context) { }
    }
}