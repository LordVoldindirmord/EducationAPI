using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Payment
{
    /// <summary>
    /// DTO для инициализации покупки роли пользователем
    /// </summary>
    public record RolePurchaseRequest
    {
        /// <summary>
        /// Название роли
        /// </summary>
        [Required(ErrorMessage ="Название роли обязательно для ее покупки")]
        [MaxLength(20, ErrorMessage ="Название роли не может быть больше 20 символов")]
        public string RoleName { get; init; } = null!;

        /// <summary>
        /// URL, куда вернуть пользователя после покупки
        /// </summary>
        [Required(ErrorMessage = "URL для возврата обязательно")]
        [MaxLength(255, ErrorMessage = "Максимальная длина URL 255 символов")]
        public string ReturnUrl { get; init; } = null!;
    }
}
