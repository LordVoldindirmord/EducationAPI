namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность курса
    /// </summary>
    public class CourseEntity
    {
        /// <summary>
        /// Id курса
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Наименование курса.
        /// Максимальная длина: 20 символов
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Описание курса.
        /// Максимальная длина: 100 символов
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Адрес до главного изображения курса.
        /// Максимальная длина: 128 символов
        /// </summary>
        public string ImagePath { get; set; } = null!;

        /// <summary>
        /// Опубликован ли курс (true - курс опубликован). Пользователи могут видеть только опубликованные курсы
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Время публикации курса.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Цена курса (0 — бесплатный)
        /// </summary>
        public decimal Price { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Id категории, к которой относится курс.
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Категория, к которой принадлежит данный курс
        /// </summary>
        public CategoryEntity Category { get; set; } = null!;

        /// <summary>
        /// Модули из этого курса
        /// </summary>
        public ICollection<ModuleEntity> Modules { get; set; } = new List<ModuleEntity>();

        /// <summary>
        /// Участники курса (владелец, менеджеры, учителя)
        /// </summary>
        public ICollection<CourseMemberEntity> Members { get; set; } = new List<CourseMemberEntity>();

        /// <summary>
        /// Записи студентов на курс
        /// </summary>
        public ICollection<EnrollmentEntity> Enrollments { get; set; } = new List<EnrollmentEntity>();

        /// <summary>
        /// Покупки этого курса
        /// </summary>
        public ICollection<CoursePurchaseEntity> Purchases { get; set; } = new List<CoursePurchaseEntity>();
    }
}
