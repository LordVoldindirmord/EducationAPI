using System.ComponentModel.DataAnnotations;
using Education.Domain.Validation;

namespace Education.Domain.DTO.Request.Courses.Modules
{
    public record CreateModuleRequest
    {
        [NotEmptyGuid]
        public Guid CourseId { get; init; }

        [Required, StringLength(20)]
        public string Title { get; init; } = null!;

        [StringLength(128)]
        public string? Description { get; init; }

        [Range(1, int.MaxValue)]
        public int OrderNumber { get; init; }
    }
}
