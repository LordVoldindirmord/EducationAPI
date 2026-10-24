using System.ComponentModel.DataAnnotations;

namespace Education.Domain.DTO.Request.Payment
{
    /// <summary>
    /// DTO для оформления возврата средств
    /// </summary>
    public record RefundRequest
    {
        /// <summary>
        /// Id транзакции (платежа), по которому нужно вернуть деньги
        /// </summary>
        [Required(ErrorMessage = "Id транзакции обязательно для возврата")]
        public string TransactionId { get; init; } = null!;
    }
}
