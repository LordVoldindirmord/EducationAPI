using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Courses.Category
{
    public record DeleteCategoryRequest
    {
        [Required(ErrorMessage = "Id категории обязательно для ее удаления")]
        public Guid CategoryId { get; init; }
    }
}
