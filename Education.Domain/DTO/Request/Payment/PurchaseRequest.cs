using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Payment
{
    /// <summary>
    /// DTO для инициализации покупки курса пользователем
    /// </summary>
    public record PurchaseRequest
    {
        /// <summary>
        /// Id курса, который хочет купить пользователь
        /// </summary>
        [Required(ErrorMessage ="Id курса обязательно для его покупки")]
        public Guid CourseId { get; init; }

        /// <summary>
        /// URL, куда вернуть пользователя после покупки
        /// </summary>
        [Required(ErrorMessage ="URL для возврата обязательно")]
        [MaxLength(255, ErrorMessage ="Максимальная длина URL 255 символов")]
        public string ReturnUrl { get; init; } = null!;
    }
}