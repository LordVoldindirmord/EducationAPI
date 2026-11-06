using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Courses.Lessons
{
    public record TestQuestionRequest : IValidatableObject
    {
        [Required, StringLength(2048)]
        public string QuestionText { get; init; } = null!;

        [Required, MinLength(2)]
        public List<string> Options { get; init; } = new();

        [Required, MinLength(1)]
        public List<int> CorrectAnswers { get; init; } = new();

        [Range(1, int.MaxValue)]
        public int OrderNumber { get; init; }

        [StringLength(1024)]
        public string? Explanation { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Options == null || Options.Any(string.IsNullOrWhiteSpace))
                yield return new ValidationResult("Варианты ответов не должны быть пустыми", new[] { nameof(Options) });

            if (CorrectAnswers != null && (CorrectAnswers.Distinct().Count() != CorrectAnswers.Count
                || CorrectAnswers.Any(i => i < 0 || Options == null || i >= Options.Count)))
                yield return new ValidationResult("Индексы правильных ответов должны быть уникальны и соответствовать вариантам", new[] { nameof(CorrectAnswers) });
        }
    }
}
