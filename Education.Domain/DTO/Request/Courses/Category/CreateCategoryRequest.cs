using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Courses.Category
{
    public record CreateCategoryRequest
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [MaxLength(20, ErrorMessage = "Максимальная длина названия категории 20 символов")]
        public string Name { get; init; } = null!;

        [MaxLength(100, ErrorMessage = "Максимальная длина описания категории 100 символов")]
        public string? Description { get; init; }
    }
}
