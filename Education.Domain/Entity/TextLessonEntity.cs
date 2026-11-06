namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность текстового контента урока
    /// </summary>
    public class TextLessonEntity : LessonEntity
    {
        /// <summary>
        /// Текстовое содержимое урока.
        /// Максимальная длина: 32192 символов
        /// </summary>
        public string Content { get; set; } = null!;

        // ========================= FK =========================
    }
}