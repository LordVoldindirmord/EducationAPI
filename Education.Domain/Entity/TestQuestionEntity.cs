namespace Education.Domain.Entity
{
    /// <summary>
    /// Вопрос теста.
    /// <para>
    /// Связь M:1 с <see cref="QuizLessonEntity"/>. Один тест может
    /// содержать много вопросов.
    /// </para>
    /// </summary>
    public class TestQuestionEntity
    {
        /// <summary>
        /// Уникальный идентификатор вопроса
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Текст вопроса.
        /// Максимальная длина: 2048 символов
        /// </summary>
        public string QuestionText { get; set; } = null!;

        /// <summary>
        /// Варианты ответов в формате JSON:
        /// ["вариант A", "вариант B", "вариант C", "вариант D"]
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>
        /// Индексы правильных ответов в формате JSON:
        /// [0] — один ответ, [0, 2] — несколько
        /// </summary>
        public List<int> CorrectAnswers { get; set; } = new List<int>();

        /// <summary>
        /// Порядковый номер вопроса в тесте (начиная с 1)
        /// </summary>
        public required int OrderNumber { get; set; }

        /// <summary>
        /// Пояснение к правильному ответу (показывается после ответа).
        /// Максимальная длина: 1024 символа
        /// </summary>
        public string? Explanation { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Идентификатор теста, к которому относится вопрос
        /// </summary>
        public Guid QuizLessonId { get; set; }

        /// <summary>
        /// Тест, к которому относится вопрос
        /// </summary>
        public QuizLessonEntity QuizLesson { get; set; } = null!;
    }
}
