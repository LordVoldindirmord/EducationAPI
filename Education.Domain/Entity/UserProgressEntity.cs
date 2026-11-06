namespace Education.Domain.Entity
{
    /// <summary>
    /// Прогресс студента по урокам.
    /// <para>
    /// Связь M:1 между <see cref="UserEntity"/> и <see cref="LessonEntity"/>
    /// </para>
    /// <para>
    /// Один пользователь - одна запись на урок. Пара UserId и LessonId уникальна в БД.
    /// Запись хранит завершение Video/Text или результат последней попытки Quiz.
    /// </para>
    /// </summary>
    public class UserProgressEntity
    {
        /// <summary>
        /// Уникальный идентификатор записи прогресса
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата и время завершения Video/Text или последней попытки Quiz, включая неуспешную.
        /// Сервис записывает UTC.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Оценка за последнюю попытку теста в процентах.
        /// CHECK в Configuration: null или целое число от 0 до 100 включительно.
        /// Для Video/Text используется null; для старых записей тестов оценка может быть неизвестна.
        /// </summary>
        public int? Score { get; set; }

        /// <summary>
        /// Пройден ли урок. Для Video/Text устанавливается true при завершении.
        /// Для Quiz устанавливается по условию Score >= PassingScore и обновляется при каждой попытке.
        /// Обязательное поле в БД. Старые записи прогресса сохраняются как пройденные при миграции.
        /// </summary>
        public bool IsPassed { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Идентификатор пользователя. При удалении пользователя прогресс удаляется каскадно.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Пользователь-студент
        /// </summary>
        public UserEntity User { get; set; } = null!;

        /// <summary>
        /// Идентификатор урока. Связь с уроком настроена через DeleteBehavior.Restrict:
        /// перед удалением урока необходимо удалить его прогресс.
        /// </summary>
        public Guid LessonId { get; set; }

        /// <summary>
        /// Урок, к которому относится прогресс
        /// </summary>
        public LessonEntity Lesson { get; set; } = null!;
    }
}
