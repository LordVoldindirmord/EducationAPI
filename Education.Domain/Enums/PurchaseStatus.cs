namespace Education.Domain.Enums
{
    /// <summary>
    /// Статус покупки
    /// </summary>
    public enum PurchaseStatus
    {
        /// <summary>
        /// Ожидает оплаты
        /// </summary>
        Pending,

        /// <summary>
        /// Успешно завершена
        /// </summary>
        Completed,

        /// <summary>
        /// Отклонена
        /// </summary>
        Failed,

        /// <summary>
        /// Возвращена
        /// </summary>
        Refunded,
    }
}
