using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.Extensions;
using Education.Domain.ResponseModel;
using Education.Service.Common.Interfaces;
using Education.Service.Purchases.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Education.Service.Purchases.Implementations
{
    public class RolePurchaseHandler : IRolePurchaseHandler
    {
        private readonly RoleManager<AppRoleEntity> _roleManager;
        private readonly UserManager<UserEntity> _userManager;
        private readonly IPermissionService _permissionService;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly EducationDbContext _context;

        public RolePurchaseHandler(
            UserManager<UserEntity> userManager,
            RoleManager<AppRoleEntity> roleManager,
            IPermissionService permissionService,
            IPurchaseRepository purchaseRepository,
            EducationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _permissionService = permissionService;
            _purchaseRepository = purchaseRepository;
            _context = context;
        }

        public async Task<InternalResponse<AppRoleEntity>> GetRoleForPurchaseAsync(Guid userId, string roleName)
        {
            // Полчаем текущую роль пользователя
            var userRole = await _permissionService.GetUserRoleAsync(userId);

            if (userRole == null)
                return CreatorResponse.Conflict<AppRoleEntity>("Пользователь или его роль не найдены");

            if (userRole == UserRole.Admin || userRole == UserRole.Moderator)
                return CreatorResponse.Conflict<AppRoleEntity>(
                    "Пользователь с ролью Администратор или Модератор не может менять роль");

            // Проверка корректности желаемой роли
            var desiredRole = GetUserRoleFromString(roleName);

            if (desiredRole == null)
                return CreatorResponse.BadRequest<AppRoleEntity>("Некорректная роль");

            if (desiredRole == userRole)
                return CreatorResponse.Conflict<AppRoleEntity>("Пользователь уже имеет эту роль");

            // Проверка роли в БД
            var role = await _roleManager.FindByNameAsync(desiredRole.Value.RoleToDbString());

            if (role == null)
                return CreatorResponse.NotFound<AppRoleEntity>("Роль не найдена в системе");

            if (role.Price <= 0)
                return CreatorResponse.Conflict<AppRoleEntity>("Эта роль бесплатная, покупка не требуется");

            // Проверка, не куплена ли уже эта роль пользователем
            if (await _purchaseRepository.ExistsCompletedRoleAsync(userId, role.Id))
                return CreatorResponse.Conflict<AppRoleEntity>("Вы уже купили эту роль");

            return CreatorResponse.Ok(role);
        }

        public async Task<InternalResponse<bool>> CompleteRolePurchaseAsync(PurchaseEntity purchase)
        {
            // Проверка, что это точно сущность покупки роли
            if (purchase is not RolePurchaseEntity rolePurchase)
                return CreatorResponse.InternalError<bool>("Неверный тип покупки (ожидалась роль)");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Меняем статус
                purchase.Status = PurchaseStatus.Completed;
                purchase.CompletedAt = DateTime.UtcNow;

                _purchaseRepository.Update(purchase);
                await _context.SaveChangesAsync();

                // Выдаем роль
                var user = await _userManager.FindByIdAsync(rolePurchase.UserId.ToString());
                var role = await _roleManager.FindByIdAsync(rolePurchase.RoleId.ToString());

                if (user == null || role == null)
                {
                    await dbTransaction.RollbackAsync();

                    return CreatorResponse.InternalError<bool>("Пользователь или роль не найдены");
                }

                // Сначала удаляем старые роли (например, User), потом выдаем новую
                // Role : User связь 1:1, это валидируется исключительно в Service слое
                var currentRoles = await _userManager.GetRolesAsync(user);

                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                var result = await _userManager.AddToRoleAsync(user, role.Name!);

                if (!result.Succeeded)
                {
                    await dbTransaction.RollbackAsync();

                    return CreatorResponse.InternalError<bool>("Не удалось выдать роль");
                }

                await dbTransaction.CommitAsync();

                return CreatorResponse.Ok(true, "Оплата прошла успешно, роль выдана");
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();

                return CreatorResponse.InternalError<bool>("Ошибка при завершении покупки роли");
            }
        }

        public async Task<InternalResponse<bool>> RevokeRoleAccessAsync(PurchaseEntity purchase)
        {
            if (purchase is not RolePurchaseEntity rolePurchase)
                return CreatorResponse.InternalError<bool>("Неверный тип покупки (ожидалась роль)");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Меняем статус на Refunded
                purchase.Status = PurchaseStatus.Refunded;

                _context.Purchase.Update(purchase);
                await _context.SaveChangesAsync();

                // Забираем роль
                var user = await _userManager.FindByIdAsync(rolePurchase.UserId.ToString());
                var role = await _roleManager.FindByIdAsync(rolePurchase.RoleId.ToString());

                if (user != null && role != null)
                {
                    // Забираем купленную роль
                    await _userManager.RemoveFromRoleAsync(user, role.Name!);

                    //  Возвращаем базовую роль User
                    var userRole = await _roleManager.FindByNameAsync(UserRole.User.RoleToDbString());

                    if (userRole != null)
                    {
                        await _userManager.AddToRoleAsync(user, userRole.Name!);
                    }
                }

                await dbTransaction.CommitAsync();

                return CreatorResponse.Ok(true, "Возврат оформлен, роль забрана");
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return CreatorResponse.InternalError<bool>("Ошибка при отзыве роли");
            }
        }

        /// <summary>
        /// Получить роль из строки
        /// </summary>
        /// <param name="role">Роль</param>
        /// <returns>Роль</returns>
        private static UserRole? GetUserRoleFromString(string role)
        {
            try
            {
                return role.DbStringToRole();
            }
            catch
            {
                return null;
            }
        }
    }
}