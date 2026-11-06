using Education.Domain.Validation;

namespace Education.Domain.DTO.Request.Courses.Lessons
{
    public record DeleteLessonRequest
    {
        [NotEmptyGuid]
        public Guid LessonId { get; init; }
    }
}
