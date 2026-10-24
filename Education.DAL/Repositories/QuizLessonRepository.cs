using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class QuizLessonRepository : BaseRepository<QuizLessonEntity>, IQuizLessonRepository
    {
        public QuizLessonRepository(EducationDbContext context) : base(context) { }
    }
}