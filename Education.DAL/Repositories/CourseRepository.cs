using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class CourseRepository : BaseRepository<CourseEntity>, ICourseRepository
    {
        public CourseRepository(EducationDbContext context) : base(context) { }
    }
}
