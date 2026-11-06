using System.ComponentModel.DataAnnotations;
using Education.Domain.Validation;

namespace Education.Domain.DTO.Request.Courses.Modules
{
    public record UpdateModuleRequest
    {
        [NotEmptyGuid]
        public Guid ModuleId { get; init; }

        [Required, StringLength(20)]
        public string NewTitle { get; init; } = null!;

        [StringLength(128)]
        public string? NewDescription { get; init; }

        [Range(1, int.MaxValue)]
        public int NewOrderNumber { get; init; }
    }
}
