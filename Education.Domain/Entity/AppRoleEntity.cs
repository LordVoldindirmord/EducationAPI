using Microsoft.AspNetCore.Identity;

namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность роли пользователя
    /// <list type="number">
    ///     <item>Admin - администратор платформы. Полный доступ ко всему.</item>
    ///     <item>Moderator - модератор платформы. Может всё, кроме работы с глобальными ролями выше Teacher.</item>
    ///     <item>Teacher - учитель. Может создавать курсы на платформе и управлять ими.</item>
    ///     <item>Student - ученик. Имеет доступ только к своим курсам.</item>
    /// </list>
    /// </summary>
    public class AppRoleEntity : IdentityRole<Guid>
    {
        /// <summary>
        /// Описание роли.
        /// Максимальная длина - 256 символов
        /// </summary>
        public string RoleDescription { get; set; } = null!;

        /// <summary>
        /// Цена роли (0 — бесплатная)
        /// </summary>
        public decimal Price { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Покупки данной роли
        /// </summary>
        public ICollection<RolePurchaseEntity> Purchases { get; set; } = new List<RolePurchaseEntity>();
    }
}
