using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Auth
{
    public record LoginRequest
    {
        [Required(ErrorMessage = "Login или Email обязателен для входа в аккаунт")]
        [MaxLength(256, ErrorMessage = "Максимальная длина Email или Login 256 символов")]
        public string LoginOrEmail { get; init; } = null!;

        [Required(ErrorMessage = "Пароль обязателен для входа в аккаунт")]
        public string Password { get; init; } = null!;

        public bool RememberMe { get; init; }
    }
}
