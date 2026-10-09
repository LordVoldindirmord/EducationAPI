using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class LessonRepository : BaseRepository<LessonEntity>, ILessonRepository
    {
        public LessonRepository(EducationDbContext context) : base(context) { }
    }
}
