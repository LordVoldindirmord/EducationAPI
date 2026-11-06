using System.ComponentModel.DataAnnotations;
using Education.Domain.Validation;

namespace Education.Domain.DTO.Request.Courses.Progress
{
    public record SubmitQuizRequest
    {
        [NotEmptyGuid]
        public Guid LessonId { get; init; }

        [Required, MinLength(1)]
        public List<UserAnswer> Answers { get; init; } = new();
    }
}
