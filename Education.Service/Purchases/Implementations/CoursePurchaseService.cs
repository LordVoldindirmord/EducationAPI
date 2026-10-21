using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.InternalServiceModels.Payments;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;
using Education.Service.Purchases.Interfaces;
using Microsoft.Extensions.Logging;

namespace Education.Service.Purchases.Implementations
{
    public class CoursePurchaseService : ICoursePurchaseService
    {
        private readonly ICoursePurchaseRepository _purchaseRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentService _enrollmentService;
        private readonly IPaymentService _paymentService;
        // Контекст используем на прямую из-за транзакций
        private readonly EducationDbContext _context;
        private readonly ILogger<CoursePurchaseService> _logger;

        public CoursePurchaseService(
            ICoursePurchaseRepository purchaseRepository,
            ICourseRepository courseRepository,
            IEnrollmentService enrollmentService,
            IPaymentService paymentService,
            EducationDbContext context,
            ILogger<CoursePurchaseService> logger)
        {
            _purchaseRepository = purchaseRepository;
            _courseRepository = courseRepository;
            _enrollmentService = enrollmentService;
            _paymentService = paymentService;
            _context = context;
            _logger = logger;
        }

        public async Task<InternalResponse<bool>> CompletePurchaseAsync(string paymentId, string transactionId)
        {
            // Ищем покупку по ID платежа из ЮKassa
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(paymentId);
            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена.");

            if (purchase.Status == PurchaseStatus.Completed)
                return CreatorResponse.Ok(true, "Покупка уже завершена."); // Идемпотентность

            if (purchase.Status != PurchaseStatus.Pending)
                return CreatorResponse.BadRequest<bool>($"Нельзя завершить покупку в статусе {purchase.Status}.");

            // Подтверждаем платеж в шлюзе (проверка на подделку)
            var confirmResult = await _paymentService.ConfirmPaymentAsync(paymentId);
            if (!confirmResult.IsSuccess)
                return confirmResult;

            // Транзакция: меняем статус + выдаем доступ
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Меняем статус
                purchase.Status = PurchaseStatus.Completed;
                purchase.CompletedAt = DateTime.UtcNow;

                _purchaseRepository.Update(purchase);
                await _purchaseRepository.SaveChangesAsync();

                // Выдаем доступ (Enrollment)
                var enrollResult = await _enrollmentService.EnrollAsync(purchase.UserId, purchase.CourseId);

                // Если доступ уже был выдан (Conflict) - это не ошибка для нас
                if (!enrollResult.IsSuccess && enrollResult.StatusCode != StatusCode.Conflict)
                {
                    await dbTransaction.RollbackAsync();

                    return CreatorResponse.InternalError<bool>($"Не удалось выдать доступ: {enrollResult.Description}");
                }

                await dbTransaction.CommitAsync();
                return CreatorResponse.Ok(true, "Оплата прошла успешно, доступ к курсу открыт.");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при завершении покупки {PurchaseId}", purchase.Id);
                return CreatorResponse.InternalError<bool>("Ошибка при обработке платежа.");
            }
        }

        public async Task<InternalResponse<bool>> FailPurchaseAsync(string paymentId, string? reason = null)
        {
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(paymentId);
            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена.");

            if (purchase.Status != PurchaseStatus.Pending)
                return CreatorResponse.Ok(true, "Покупка уже обработана.");

            purchase.Status = PurchaseStatus.Failed;
            _purchaseRepository.Update(purchase);
            await _purchaseRepository.SaveChangesAsync();

            await _paymentService.FailPaymentAsync(paymentId, reason);
            return CreatorResponse.Ok(true, "Покупка отменена.");
        }

        public async Task<InternalResponse<PaymentResult>> InitiatePurchaseAsync(Guid userId, Guid courseId, string returnUrl)
        {
            // Проверка курса
            var course = await _courseRepository.GetByIdAsync(courseId);

            if (course == null)
                return CreatorResponse.NotFound<PaymentResult>("Курс не найден");

            if (!course.IsPublished)
                return CreatorResponse.Conflict<PaymentResult>("Нельзя купить неопубликованный курс");

            if (course.Price <= 0)
                return CreatorResponse.Conflict<PaymentResult>("Курс бесплатный, покупка не требуется");

            // Проверка, что курс еще не куплен
            if (await _purchaseRepository.ExistsCompletedAsync(userId, courseId))
                return CreatorResponse.Conflict<PaymentResult>("Вы уже купили этот курс");

            // Создание платежа в ЮKassa
            var paymentResult = await _paymentService.CreatePaymentAsync(
                course.Price, "RUB", $"Покупка курса '{course.Title}'", returnUrl);

            if (!paymentResult.IsSuccess)
                return paymentResult;

            // Создание записи в БД
            var purchase = new CoursePurchaseEntity
            {
                UserId = userId,
                CourseId = courseId,
                PricePaid = course.Price,
                Currency = "RUB",
                TransactionId = paymentResult.Data!.PaymentId, // ID платежа в ЮKassa
                Status = PurchaseStatus.Pending
            };

            await _purchaseRepository.CreateAsync(purchase);
            await _purchaseRepository.SaveChangesAsync();

            return paymentResult;
        }

        public async Task<InternalResponse<bool>> RefundPurchaseAsync(string paymentId)
        {
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(paymentId);
            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена.");

            if (purchase.Status != PurchaseStatus.Completed)
                return CreatorResponse.BadRequest<bool>("Вернуть можно только завершенную покупку.");

            // 1. Возвращаем деньги через шлюз
            var refundResult = await _paymentService.RefundPaymentAsync(paymentId);
            if (!refundResult.IsSuccess)
                return refundResult;

            // 2. Отзываем доступ (удаляем Enrollment)
            await _enrollmentService.UnenrollAsync(purchase.UserId, purchase.CourseId);

            // 3. Меняем статус
            purchase.Status = PurchaseStatus.Refunded;
            _purchaseRepository.Update(purchase);
            await _purchaseRepository.SaveChangesAsync();

            return CreatorResponse.Ok(true, "Возврат оформлен, доступ к курсу закрыт.");
        }
    }
}
