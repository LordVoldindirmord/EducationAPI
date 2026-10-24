using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class VideoLessonRepository : BaseRepository<VideoLessonEntity>, IVideoLessonRepository
    {
        public VideoLessonRepository(EducationDbContext context) : base(context) { }
    }
}