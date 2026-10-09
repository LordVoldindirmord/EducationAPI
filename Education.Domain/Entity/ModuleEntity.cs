namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность модуля курса
    /// </summary>
    public class ModuleEntity
    {
        /// <summary>
        /// Id модуля
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название модуля.
        /// Максимальная длина: 20 символов
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Описание модуля.
        /// Максимальная длина: 128 символов
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Порядковый номер модуля в курсе
        /// </summary>
        public required int OrderNumber { get; set; }

        /// <summary>
        /// Дата и время создания модуля.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Id курса, которому принадлежит данный модуль
        /// </summary>
        public Guid CourseId { get; set; }

        /// <summary>
        /// Курс, к которому принадлежит данный модуль
        /// </summary>
        public CourseEntity Course { get; set; } = null!;

        /// <summary>
        /// Уроки из данного модуля
        /// </summary>
        public ICollection<LessonEntity> Lessons { get; set; } = new List<LessonEntity>();
    }
}
