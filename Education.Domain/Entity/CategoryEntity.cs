namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность категории курса
    /// </summary>
    public class CategoryEntity
    {
        /// <summary>
        /// Id категории курса
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Наименование категории.
        /// Максимальная длина: 20 символов
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Описание категории курса.
        /// Максимальная длина: 100 символов
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Время создания категории.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Коллекция курсов в данной категории
        /// </summary>
        public ICollection<CourseEntity> Courses { get; set; } = new List<CourseEntity>();
    }
}
