using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Auth
{
    public record ResetPasswordRequest
    {
        [Required(ErrorMessage = "Id пользователя обязателен")]
        public Guid UserId { get; init; }

        [Required(ErrorMessage = "Токен обязателен")]
        [MaxLength(512, ErrorMessage = "Максимальная длина токена 512 символов")]
        public string Token { get; init; } = null!;

        [Required(ErrorMessage = "Пароль обязателен для заполнения")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        public string NewPassword { get; init; } = null!;

        [Required(ErrorMessage = "Пароль обязателен для заполнения")]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmNewPassword { get; init; } = null!;
    }
}