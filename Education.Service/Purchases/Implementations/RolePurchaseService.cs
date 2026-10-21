using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.InternalServiceModels.Payments;
using Education.Domain.ResponseModel;
using Education.Service.Purchases.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Education.Service.Purchases.Implementations
{
    public class RolePurchaseService : IRolePurchaseService
    {
        private readonly IRolePurchaseRepository _purchaseRepository;
        private readonly IPaymentService _paymentService;
        private readonly RoleManager<AppRoleEntity> _roleManager;
        private readonly UserManager<UserEntity> _userManager;
        private readonly EducationDbContext _context;
        private readonly ILogger<RolePurchaseService> _logger;

        public RolePurchaseService(
            IRolePurchaseRepository purchaseRepository,
            IPaymentService paymentService,
            RoleManager<AppRoleEntity> roleManager,
            UserManager<UserEntity> userManager,
            EducationDbContext context,
            ILogger<RolePurchaseService> logger)
        {
            _purchaseRepository = purchaseRepository;
            _paymentService = paymentService;
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<InternalResponse<PaymentResult>> InitiatePurchaseAsync(Guid userId, Guid roleId, string returnUrl)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
                return CreatorResponse.NotFound<PaymentResult>("Роль не найдена.");

            if (role.Price <= 0)
                return CreatorResponse.Conflict<PaymentResult>("Эта роль бесплатная или недоступна для покупки.");

            if (await _purchaseRepository.ExistsCompletedAsync(userId, roleId))
                return CreatorResponse.Conflict<PaymentResult>("Вы уже купили эту роль.");

            var paymentResult = await _paymentService.CreatePaymentAsync(
                role.Price, "RUB", $"Покупка роли '{role.Name}'", returnUrl);

            if (!paymentResult.IsSuccess)
                return paymentResult;

            var purchase = new RolePurchaseEntity
            {
                UserId = userId,
                RoleId = roleId,
                PricePaid = role.Price,
                Currency = "RUB",
                TransactionId = paymentResult.Data!.PaymentId,
                Status = PurchaseStatus.Pending
            };

            await _purchaseRepository.CreateAsync(purchase);
            await _purchaseRepository.SaveChangesAsync();

            return paymentResult;
        }

        public async Task<InternalResponse<bool>> CompletePurchaseAsync(string paymentId, string transactionId)
        {
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(paymentId);
            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена.");

            if (purchase.Status == PurchaseStatus.Completed)
                return CreatorResponse.Ok(true, "Покупка уже завершена.");

            if (purchase.Status != PurchaseStatus.Pending)
                return CreatorResponse.BadRequest<bool>($"Нельзя завершить покупку в статусе {purchase.Status}.");

            var confirmResult = await _paymentService.ConfirmPaymentAsync(paymentId);
            if (!confirmResult.IsSuccess)
                return confirmResult;

            var role = await _roleManager.FindByIdAsync(purchase.RoleId.ToString());
            var user = await _userManager.FindByIdAsync(purchase.UserId.ToString());

            if (role == null || user == null)
                return CreatorResponse.InternalError<bool>("Не удалось найти роль или пользователя.");

            // Транзакция: статус + выдача роли
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                purchase.Status = PurchaseStatus.Completed;
                purchase.CompletedAt = DateTime.UtcNow;
                _purchaseRepository.Update(purchase);
                await _purchaseRepository.SaveChangesAsync();

                // Выдаем роль
                var addRoleResult = await _userManager.AddToRoleAsync(user, role.Name!);

                // Если роль уже была (Conflict) - это не ошибка
                if (!addRoleResult.Succeeded && !addRoleResult.Errors.Any(e => e.Code == "UserAlreadyInRole"))
                {
                    await dbTransaction.RollbackAsync();
                    var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                    return CreatorResponse.InternalError<bool>($"Не удалось выдать роль: {errors}");
                }

                await dbTransaction.CommitAsync();
                return CreatorResponse.Ok(true, "Оплата прошла успешно, роль выдана.");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при завершении покупки роли {PurchaseId}", purchase.Id);
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

        public async Task<InternalResponse<bool>> RefundPurchaseAsync(string paymentId)
        {
            var purchase = await _purchaseRepository.GetByTransactionIdTrackedAsync(paymentId);
            if (purchase == null)
                return CreatorResponse.NotFound<bool>("Покупка не найдена.");

            if (purchase.Status != PurchaseStatus.Completed)
                return CreatorResponse.BadRequest<bool>("Вернуть можно только завершенную покупку.");

            var refundResult = await _paymentService.RefundPaymentAsync(paymentId);
            if (!refundResult.IsSuccess)
                return refundResult;

            var role = await _roleManager.FindByIdAsync(purchase.RoleId.ToString());
            var user = await _userManager.FindByIdAsync(purchase.UserId.ToString());

            if (role != null && user != null)
            {
                // Забираем роль (если она есть)
                await _userManager.RemoveFromRoleAsync(user, role.Name!);
            }

            purchase.Status = PurchaseStatus.Refunded;
            _purchaseRepository.Update(purchase);
            await _purchaseRepository.SaveChangesAsync();

            return CreatorResponse.Ok(true, "Возврат оформлен, роль забрана.");
        }
    }
}
