namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность покупки курса пользователем
    /// </summary>
    public class CoursePurchaseEntity : PurchaseEntity
    {
        // ========================= FK =========================

        /// <summary>
        /// Идентификатор купленного курса
        /// </summary>
        public Guid CourseId { get; set; }

        /// <summary>
        /// Купленный курс
        /// </summary>
        public CourseEntity Course { get; set; } = null!;
    }
}
