namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность видео-контента урока.
    /// </summary>
    public class VideoLessonEntity : LessonEntity
    {
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
    }
}