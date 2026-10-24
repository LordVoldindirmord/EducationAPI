namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность покупки роли пользователем
    /// </summary>
    public class RolePurchaseEntity : PurchaseEntity
    {
        // ========================= FK =========================

        /// <summary>
        /// Идентификатор купленной роли
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Купленная роль
        /// </summary>
        public AppRoleEntity Role { get; set; } = null!;
    }
}