namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность видео-контента
    /// <para>
    /// Связь 1:1 с <see cref="LessonEntity"/>. Используется, когда
    /// <see cref="LessonEntity.LessonType"/> = Video.
    /// </para>
    /// </summary>
    public class VideoLessonEntity
    {
        /// <summary>
        /// Id видео
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Путь к видео контенту.
        /// Максимальная длина: 128 символов
        /// </summary>
        public string VideoPath { get; set; } = null!;

        /// <summary>
        /// Продолжительность видео в секундах
        /// NOT RENAME (ограничение CHECK в Configuration)
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Путь к превью с изображением видео
        /// Максимальная длина: 128 символов
        /// </summary>
        public string ThumbnailPath { get; set; } = null!;

        // ========================= FK =========================

        /// <summary>
        /// Id урока (связь 1:1)
        /// </summary>
        public Guid LessonId { get; set; }

        /// <summary>
        /// Урок, к которому относится видео-контент
        /// </summary>
        public LessonEntity Lesson { get; set; } = null!;
    }
}