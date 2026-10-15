using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Auth
{
    public record RegisterRequest
    {
        [Required(ErrorMessage = "Email обязателен для заполнения")]
        [EmailAddress(ErrorMessage = "Введите корректный Email адрес")]
        [MaxLength(256, ErrorMessage = "Максимальная длина Email 256 символов")]
        public string Email { get; init; } = null!;

        [Required(ErrorMessage = "Login обязателен для заполнения")]
        [MaxLength(32, ErrorMessage = "Максимальная длина Login может быть 256 символов")]
        public string Login { get; init; } = null!;

        [Required(ErrorMessage = "Имя обязательно для заполнения")]
        [MaxLength(50, ErrorMessage = "Максимальная длина имени может быть 50 символов")]
        public string FirstName { get; init; } = null!;

        [Required(ErrorMessage = "Фамилия обязательна для заполнения")]
        [MaxLength(50, ErrorMessage = "Максимальная длина имени может быть 50 символов")]
        public string LastName { get; init; } = null!;

        [Required(ErrorMessage = "Пароль обязателен для заполнения")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        public string Password { get; init; } = null!;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; init; } = null!;
    }
}
