using System.ComponentModel.DataAnnotations;

namespace Education.Domain.Validation
{
    // Required не отклоняет Guid.Empty: это значение, а не null.
    public sealed class NotEmptyGuidAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is Guid id && id != Guid.Empty;
        }
    }
}
