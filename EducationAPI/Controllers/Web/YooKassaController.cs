using Education.Service.Purchases.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EducationAPI.Controllers.Web
{
    [Route("api/yookassa")]
    [ApiController]
    public class YooKassaController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ILogger<YooKassaController> _logger;

        public YooKassaController(
            IPurchaseService purchaseService,
            ILogger<YooKassaController> logger)
        {
            _purchaseService = purchaseService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement json)
        {
            try
            {
                // ЮKassa присылает event и object (внутри которого id)
                var eventType = json.GetProperty("event").GetString();

                var transactionId = json.GetProperty("object").GetProperty("id").GetString();

                if (string.IsNullOrEmpty(transactionId))
                {
                    _logger.LogWarning("Вебхук от ЮKassa не содержит ID платежа");

                    return BadRequest();
                }

                _logger.LogInformation($"Получен вебхук от ЮKassa: {eventType} для платежа {transactionId}");

                switch (eventType)
                {
                    case "payment.succeeded":
                        // Завершение покупки
                        await _purchaseService.CompletePurchaseAsync(transactionId);
                        break;

                    case "payment.canceled":
                        // Отмена покупки
                        await _purchaseService.FailPurchaseAsync(transactionId, "Отменено ЮKassa");
                        break;

                    case "refund.succeeded":
                        // Если возврат был сделан не через наш API (например, вручную в ЮKassa)
                        // Нам нужно найти покупку по ID возврата или платежа и обновить статус
                        // По идее у нас все это происходит уже в логике возврата, но здесь можно перестраховаться
                        _logger.LogInformation("Получен вебхук о возврате: {TransactionId}", transactionId);
                        break;

                    default:
                        // В сучае непредвиденных обстоятельств
                        _logger.LogWarning("Неизвестное событие от ЮKassa: {Event}", eventType);
                        break;
                }

                // ЮKassa ждет 200 OK, чтобы прекратить повторные попытки
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке вебхука от ЮKassa");
                // Возвращаем 500, чтобы ЮKassa повторила попытку позже
                return StatusCode(500);
            }
        }
    }
}
