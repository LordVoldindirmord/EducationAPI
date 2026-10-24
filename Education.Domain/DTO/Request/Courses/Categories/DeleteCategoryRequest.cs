using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Courses.Categories
{
    public record DeleteCategoryRequest
    {
        [Required(ErrorMessage = "Id категории обязательно для ее удаления")]
        public Guid CategoryId { get; init; }
    }
}