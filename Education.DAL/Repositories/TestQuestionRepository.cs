using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class TestQuestionRepository : BaseRepository<TestQuestionEntity>, ITestQuestionRepository
    {
        public TestQuestionRepository(EducationDbContext context) : base(context) { }
    }
}