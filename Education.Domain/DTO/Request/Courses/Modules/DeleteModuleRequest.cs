using Education.Domain.Validation;

namespace Education.Domain.DTO.Request.Courses.Modules
{
    public record DeleteModuleRequest
    {
        [NotEmptyGuid]
        public Guid ModuleId { get; init; }
    }
}
