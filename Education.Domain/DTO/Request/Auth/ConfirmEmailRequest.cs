using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Auth
{
    public record ConfirmEmailRequest
    {
        [Required(ErrorMessage = "Id обязателен")]
        public Guid UserId { get; init; }

        [Required(ErrorMessage = "Токен обязателен")]
        [MaxLength(512, ErrorMessage = "Максимальная длина токена 512 символов")]
        public string Token { get; init; } = null!;
    }
}
