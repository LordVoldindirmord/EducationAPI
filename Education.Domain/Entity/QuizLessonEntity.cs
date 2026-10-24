namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность теста урока.
    /// <para>
    /// Связь 1:1 с <see cref="LessonEntity"/>. Используется, когда
    /// <see cref="LessonEntity.LessonType"/> = Quiz.
    /// </para>
    /// </summary>
    public class QuizLessonEntity
    {
        /// <summary>
        /// Уникальный идентификатор теста
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Проходной балл в процентах (0–100)
        /// </summary>
        public int PassingScore { get; set; }

        /// <summary>
        /// Ограничение по времени в минутах (опционально)
        /// </summary>
        public int? TimeLimitMinutes { get; set; }

        /// <summary>
        /// Показывать ли ошибки сразу (true - показывать, false - не показывать)
        /// </summary>
        public bool ShowErrorsImmediately { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Идентификатор урока (связь 1:1)
        /// </summary>
        public Guid LessonId { get; set; }

        /// <summary>
        /// Урок, к которому относится тест
        /// </summary>
        public LessonEntity Lesson { get; set; } = null!;

        /// <summary>
        /// Вопросы теста
        /// </summary>
        public ICollection<TestQuestionEntity> Questions { get; set; } = new List<TestQuestionEntity>();
    }
}