using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Auth
{
    public record ResendConfirmationRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный Email")]
        [MaxLength(256, ErrorMessage = "Максимальная длина Email 256 символов")]
        public string Email { get; init; } = null!;
    }
}