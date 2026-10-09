using Education.Domain.Enums;

namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность урока курса
    /// </summary>
    public class LessonEntity
    {
        /// <summary>
        /// Id урока
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название урока.
        /// Максимальная длина: 20 символов
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Порядковый номер урока в модуле
        /// </summary>
        public required int OrderNumber { get; set; }

        /// <summary>
        /// Тип урока
        /// </summary>
        public required LessonType LessonType { get; set; }

        /// <summary>
        /// Опубликован ли урок (true - опубликован, false - нет). Пользователь может увидеть только опубликованные уроки
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Время создания урока.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Id модуля, к которому относится урок
        /// </summary>
        public Guid ModuleId { get; set; }

        /// <summary>
        /// Модуль, к которому относится урок
        /// </summary>
        public ModuleEntity Module { get; set; } = null!;

        /// <summary>
        /// Видео-контент урока
        /// </summary>
        public VideoLessonEntity? VideoLesson { get; set; }

        /// <summary>
        /// Текстовый контент урока
        /// </summary>
        public TextLessonEntity? TextLesson { get; set; }

        /// <summary>
        /// Тестовый контент урока
        /// </summary>
        public QuizLessonEntity? QuizLesson { get; set; }

        /// <summary>
        /// Прогресс пользователей по данному уроку
        /// </summary>
        public ICollection<UserProgressEntity> Progresses { get; set; } = new List<UserProgressEntity>();
    }
}
