using Education.DAL.Interfaces;
using Education.Domain.DTO.Response.Payment;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.ResponseModel;
using Education.Service.Purchases.Interfaces;

namespace Education.Service.Purchases.Implementations
{
    public class PurchaseService : IPurchaseService
    {
        // WARNING: При инициализации покупки обязательно заполнять PurchaseType
        // Без этого будет баг, платеж всегда будет идти по курсам

        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IPaymentService _paymentService;
        private readonly ICoursePurchaseHandler _coursePurchaseHandler;
        private readonly IRolePurchaseHandler _rolePurchaseHandler;

        public PurchaseService(
            IPurchaseRepository purchaseRepository,
            IPaymentService paymentService,
            ICoursePurchaseHandler coursePurchaseHandler,
            IRolePurchaseHandler rolePurchaseHandler)
        {
            _purchaseRepository = purchaseRepository;
            _paymentService = paymentService;
            _coursePurchaseHandler = coursePurchaseHandler;
            _rolePurchaseHandler = rolePurchaseHandler;
        }

        public async Task<InternalResponse<bool>> CompletePurchaseAsync(string transactionId)
        {
            // Валидация самой покупки
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(transactionId);

            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена");

            if (purchase.Status == PurchaseStatus.Completed)
                return CreatorResponse.Ok(true, "Покупка уже завершена");

            if (purchase.Status != PurchaseStatus.Pending)
                return CreatorResponse.BadRequest<bool>($"Нельзя завершить покупку в статусе {purchase.Status}");

            // Подтверждение платежа (если будет подделка, то оплаты не будет)
            var confirmResult = await _paymentService.ConfirmPaymentAsync(transactionId);

            if (!confirmResult.IsSuccess)
                return confirmResult;

            // Проведение транзакции в БД
            return purchase.PurchaseType switch
            {
                PurchaseType.Course => await _coursePurchaseHandler.CompleteCoursePurchaseAsync(purchase),
                PurchaseType.Role => await _rolePurchaseHandler.CompleteRolePurchaseAsync(purchase),
                _ => CreatorResponse.InternalError<bool>($"Неизвестный тип платежа: {purchase.PurchaseType}")
            };
        }

        public async Task<InternalResponse<bool>> FailPurchaseAsync(string transactionId, string? reason = null)
        {
            // Получаем покупку
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(transactionId);

            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена");

            if (purchase.Status != PurchaseStatus.Pending)
                return CreatorResponse.Ok(true, "Покупка уже обработана");

            // Изменяем статус
            purchase.Status = PurchaseStatus.Failed;

            _purchaseRepository.Update(purchase);
            await _purchaseRepository.SaveChangesAsync();

            // Отмечаем платеж, как неудачный, в платежной системе
            await _paymentService.FailPaymentAsync(transactionId, reason);

            return CreatorResponse.Ok(true, "Покупка отменена");
        }

        public async Task<InternalResponse<PaymentResult>> InitiateCoursePurchaseAsync(
            Guid userId, Guid courseId, string returnUrl)
        {
            // Получение курса, если пользователь его может купить
            var response = await _coursePurchaseHandler.GetCourseForPurchaseAsync(userId, courseId);

            // Парсинг в ответ, если что-то пошло не так
            if (!response.IsSuccess)
                return ParseInvalidInternalResponse<PaymentResult, CourseEntity>(response);

            // Создание платежа
            var paymentResult = await _paymentService
                .CreatePaymentAsync(response.Data!.Price, "RUB", $"Покупка курса '{response.Data!.Title}'", returnUrl);

            if (!paymentResult.IsSuccess)
                return paymentResult;

            // Создание записи в БД
            var purchase = new CoursePurchaseEntity
            {
                UserId = userId,
                CourseId = courseId,
                PricePaid = response.Data!.Price,
                Currency = "RUB",
                TransactionId = paymentResult.Data!.TransactionId,
                Status = PurchaseStatus.Pending,
                PurchaseType = PurchaseType.Course
            };

            await _purchaseRepository.CreateAsync(purchase);
            await _purchaseRepository.SaveChangesAsync();

            return CreatorResponse.Ok(paymentResult.Data, "Платеж успешно создан, переходите по ссылке для оплаты");
        }

        public async Task<InternalResponse<PaymentResult>> InitiateRolePurchaseAsync(Guid userId, string roleName, string returnUrl)
        {
            // Получение роли, если пользователь ее может купить
            var role = await _rolePurchaseHandler.GetRoleForPurchaseAsync(userId, roleName);

            // Парсинг в ответ, если что-то пошло не так
            if (!role.IsSuccess)
                return ParseInvalidInternalResponse<PaymentResult, AppRoleEntity>(role);

            // Создание платежа
            var paymentResult = await _paymentService.CreatePaymentAsync(
                role.Data!.Price, "RUB", $"Покупка роли '{role.Data!.Name}'", returnUrl);

            if (!paymentResult.IsSuccess)
                return paymentResult;

            // Создание записи в БД
            var purchase = new RolePurchaseEntity
            {
                UserId = userId,
                RoleId = role.Data!.Id,
                PricePaid = role.Data!.Price,
                Currency = "RUB",
                TransactionId = paymentResult.Data!.TransactionId,
                Status = PurchaseStatus.Pending,
                PurchaseType = PurchaseType.Role
            };

            await _purchaseRepository.CreateAsync(purchase);
            await _purchaseRepository.SaveChangesAsync();

            return CreatorResponse.Ok(paymentResult.Data, "Платеж успешно создан, переходите по ссылке для оплаты");
        }

        public async Task<InternalResponse<bool>> RefundPurchaseAsync(string transactionId)
        {
            // Получаем саму покупку
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(transactionId);

            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена");

            if (purchase.Status != PurchaseStatus.Completed)
                return CreatorResponse.BadRequest<bool>("Вернуть можно только завершенную покупку");

            // Возвращаем деньги через шлюз
            var refundResult = await _paymentService.RefundPaymentAsync(transactionId);

            if (!refundResult.IsSuccess)
                return refundResult;

            // Проведение транзакции в БД
            return purchase.PurchaseType switch
            {
                PurchaseType.Course => await _coursePurchaseHandler.RevokeCourseAccessAsync(purchase),
                PurchaseType.Role => await _rolePurchaseHandler.RevokeRoleAccessAsync(purchase),
                _ => CreatorResponse.InternalError<bool>($"Неизвестный тип платежа: {purchase.PurchaseType}")
            };
        }

        /// <summary>
        /// Парсинг из InternalResponse в InternalResponse, но с другим типом (тип будет null)
        /// </summary>
        /// <typeparam name="T1">Требуемый тип InternalResponse</typeparam>
        /// <typeparam name="T2">Тип данных в InternalResponse, из которого будем парсить</typeparam>
        /// <param name="response">Исходный InternalResponse</param>
        /// <returns>Итоговый InternalResponse</returns>
        private static InternalResponse<T1> ParseInvalidInternalResponse<T1, T2>(InternalResponse<T2> response) =>
            new InternalResponse<T1>
            {
                Description = response.Description,
                StatusCode = response.StatusCode
            };
    }
}