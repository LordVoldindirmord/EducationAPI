using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Courses.Progress
{
    public record UserAnswer
    {
        [Range(1, int.MaxValue)]
        public int QuestionOrderNumber { get; init; }

        [Required]
        public List<int> SelectedOptions { get; init; } = new();
    }
}
