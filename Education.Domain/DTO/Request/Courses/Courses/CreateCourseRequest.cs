using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Education.Domain.Validation;
using Microsoft.AspNetCore.Http;

namespace Education.Domain.DTO.Request.Courses.Courses
{
    public record CreateCourseRequest
    {
        [Required(ErrorMessage = "Название курса обязательно")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Название курса должно быть от 5 до 20 символов")]
        public string Title { get; init; } = null!;

        [StringLength(100, MinimumLength = 20, ErrorMessage = "Описание курса должно быть от 20 до 100 символов")]
        public string? Description { get; init; }

        [Required(ErrorMessage = "Обложка курса обязательна")]
        public IFormFile Image { get; init; } = null!;

        public bool IsPublished { get; init; }

        [DefaultValue(0)]
        [Range(typeof(decimal), "0", "9999999.99", ParseLimitsInInvariantCulture = true,
            ErrorMessage = "Цена должна быть от 0 до 9999999.99")]
        public decimal Price { get; init; }

        [NotEmptyGuid(ErrorMessage = "Id категории не должен быть пустым")]
        public Guid CategoryId { get; init; }
    }
}
