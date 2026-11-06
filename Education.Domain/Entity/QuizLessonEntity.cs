namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность теста урока
    /// </summary>
    public class QuizLessonEntity : LessonEntity
    {
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
        /// Вопросы теста
        /// </summary>
        public ICollection<TestQuestionEntity> Questions { get; set; } = new List<TestQuestionEntity>();
    }
}