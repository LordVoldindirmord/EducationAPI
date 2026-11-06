using System.ComponentModel.DataAnnotations;
using Education.Domain.Enums;
using Education.Domain.Validation;
using Microsoft.AspNetCore.Http;

namespace Education.Domain.DTO.Request.Courses.Lessons
{
    public record UpdateLessonRequest
    {
        [NotEmptyGuid]
        public Guid LessonId { get; init; }

        [Required, StringLength(20)]
        public string NewTitle { get; init; } = null!;

        [Range(1, int.MaxValue)]
        public int NewOrderNumber { get; init; }

        // Тип и модуль урока сохраняются; null в полях контента сохраняет текущее значение.
        public bool RemoveTimeLimit { get; init; }

        public bool IsPublished { get; init; }
        public IFormFile? NewVideoFile { get; init; }
        public IFormFile? NewThumbnailFile { get; init; }

        [Range(1, int.MaxValue)]
        public int? NewDurationSeconds { get; init; }

        [StringLength(32192)]
        public string? NewTextContent { get; init; }

        public List<TestQuestionRequest>? NewQuestions { get; init; }

        [Range(0, 100)]
        public int? NewPassingScore { get; init; }

        [Range(1, int.MaxValue)]
        public int? NewTimeLimitMinutes { get; init; }

        public bool? NewShowErrorsImmediately { get; init; }
    }
}
