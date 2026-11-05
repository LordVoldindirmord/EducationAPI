namespace Education.Domain.DTO.Request.Courses.Courses
{
    using System.ComponentModel.DataAnnotations;
    using Education.Domain.Validation;
    using Microsoft.AspNetCore.Http;

    public record UpdateCourseRequest
    {
        [NotEmptyGuid(ErrorMessage = "Id курса не должен быть пустым")]
        public Guid CourseId { get; init; }

        [Required(ErrorMessage = "Название курса обязательно")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Название курса должно быть от 5 до 20 символов")]
        public string NewTitle { get; init; } = null!;

        [StringLength(100, MinimumLength = 20, ErrorMessage = "Описание курса должно быть от 20 до 100 символов")]
        public string? NewDescription { get; init; }

        public IFormFile? NewImage { get; init; }

        [Range(typeof(decimal), "0", "9999999.99", ParseLimitsInInvariantCulture = true,
            ErrorMessage = "Цена должна быть от 0 до 9999999.99")]
        public decimal NewPrice { get; init; }

        [NotEmptyGuid(ErrorMessage = "Id категории не должен быть пустым")]
        public Guid NewCategoryId { get; init; }
    }
}
