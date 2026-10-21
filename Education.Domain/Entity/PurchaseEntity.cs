using Education.Domain.Enums;

namespace Education.Domain.Entity
{
    /// <summary>
    /// Базовая сущность покупки
    /// </summary>
    public class PurchaseEntity
    {
        /// <summary>
        /// Уникальный идентификатор покупки
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Оплаченная цена на момент покупки
        /// </summary>
        public decimal PricePaid { get; set; }

        /// <summary>
        /// Валюта платежа.
        /// Максимальная длина: 3 символа
        /// </summary>
        public string Currency { get; set; } = "RUB";

        /// <summary>
        /// Идентификатор транзакции от платёжной системы.
        /// Максимальная длина: 128 символов
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Способ оплаты (Card, SBP, ...).
        /// Максимальная длина: 20 символов
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Статус платежа
        /// </summary>
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;

        /// <summary>
        /// Дата и время создания записи о покупке.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата и время успешной оплаты.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Тип, на что именно была произведена оплата
        /// </summary>
        public PurchaseType PurchaseType { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Идентификатор пользователя-покупателя
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Пользователь-покупатель
        /// </summary>
        public UserEntity User { get; set; } = null!;
    }
}
