namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность текстового контента урока.
    /// <para>
    /// Связь 1:1 с <see cref="LessonEntity"/>. Используется, когда
    /// <see cref="LessonEntity.LessonType"/> = Text.
    /// </para>
    /// </summary>
    public class TextLessonEntity
    {
        /// <summary>
        /// Уникальный идентификатор текстового контента
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Текстовое содержимое урока.
        /// Максимальная длина: 32192 символов
        /// </summary>
        public string Content { get; set; } = null!;

        // ========================= FK =========================

        /// <summary>
        /// Идентификатор урока (связь 1:1)
        /// </summary>
        public Guid LessonId { get; set; }

        /// <summary>
        /// Урок, к которому относится текстовый контент
        /// </summary>
        public LessonEntity Lesson { get; set; } = null!;
    }
}
