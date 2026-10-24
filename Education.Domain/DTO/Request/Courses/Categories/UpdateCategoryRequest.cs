using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Courses.Categories
{
    public record UpdateCategoryRequest
    {
        public Guid CategoryId { get; init; }

        [Required(ErrorMessage = "Название категории обязательно")]
        [MaxLength(20, ErrorMessage = "Максимальная длина названия категории 20 символов")]
        public string NewName { get; init; } = null!;

        [MaxLength(100, ErrorMessage = "Максимальная длина описания категории 100 символов")]
        public string? NewDescription { get; init; }
    }
}