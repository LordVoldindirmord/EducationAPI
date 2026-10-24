using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class TextLessonRepository : BaseRepository<TextLessonEntity>, ITextLessonRepository
    {
        public TextLessonRepository(EducationDbContext context) : base(context) { }
    }
}