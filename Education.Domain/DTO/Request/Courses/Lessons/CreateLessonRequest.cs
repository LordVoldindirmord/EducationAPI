using System.ComponentModel.DataAnnotations;
using Education.Domain.Enums;
using Education.Domain.Validation;
using Microsoft.AspNetCore.Http;

namespace Education.Domain.DTO.Request.Courses.Lessons
{
    public record CreateLessonRequest
    {
        [NotEmptyGuid]
        public Guid ModuleId { get; init; }

        [Required, StringLength(20)]
        public string Title { get; init; } = null!;

        [Range(1, int.MaxValue)]
        public int OrderNumber { get; init; }

        [EnumDataType(typeof(LessonType))]
        public LessonType LessonType { get; init; }

        public bool IsPublished { get; init; }
        public IFormFile? VideoFile { get; init; }
        public IFormFile? ThumbnailFile { get; init; }

        [Range(1, int.MaxValue)]
        public int? DurationSeconds { get; init; }

        [StringLength(32192)]
        public string? TextContent { get; init; }

        public List<TestQuestionRequest>? Questions { get; init; }

        [Range(0, 100)]
        public int PassingScore { get; init; }

        [Range(1, int.MaxValue)]
        public int? TimeLimitMinutes { get; init; }

        public bool ShowErrorsImmediately { get; init; }
    }
}
