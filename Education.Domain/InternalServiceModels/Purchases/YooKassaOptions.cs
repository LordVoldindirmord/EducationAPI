namespace Education.Domain.InternalServiceModels.Purchases
{
    /// <summary>
    /// Настройки для подключения к ЮKassa.
    /// </summary>
    public class YooKassaOptions
    {
        public string ShopId { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
    }
}