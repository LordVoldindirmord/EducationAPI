namespace Education.Domain.Entity
{
    /// <summary>
    /// Прогресс студента по урокам.
    /// <para>
    /// Связь M:1 между <see cref="UserEntity"/> и <see cref="LessonEntity"/>
    /// </para>
    /// <para>
    /// Один студент — один прогресс на урок. Запись появляется только после завершения урока.
    /// </para>
    /// </summary>
    public class UserProgressEntity
    {
        /// <summary>
        /// Уникальный идентификатор записи прогресса
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата и время завершения урока.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CompletedAt { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Идентификатор пользователя-студента
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Пользователь-студент
        /// </summary>
        public UserEntity User { get; set; } = null!;

        /// <summary>
        /// Идентификатор урока
        /// </summary>
        public Guid LessonId { get; set; }

        /// <summary>
        /// Урок, к которому относится прогресс
        /// </summary>
        public LessonEntity Lesson { get; set; } = null!;
    }
}