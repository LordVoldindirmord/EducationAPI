namespace Education.Domain.Entity
{
    /// <summary>
    /// Запись студента на курс.
    /// <para>
    /// В отличие от <see cref="CourseMemberEntity"/> (управляющие курсом),
    /// эта сущность описывает <b>студентов</b>, которые проходят курс.
    /// </para>
    /// <para>
    /// Связь M:N между <see cref="UserEntity"/> и <see cref="CourseEntity"/>
    /// с дополнительными данными о прогрессе обучения.
    /// </para>
    /// <para>
    /// Один пользователь может быть записан на несколько курсов,
    /// и на одном курсе может быть много студентов.
    /// </para>
    /// </summary>
    public class EnrollmentEntity
    {
        /// <summary>
        /// Уникальный идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата и время записи студента на курс.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime EnrolledAt { get; set; }

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
        /// Идентификатор курса, на который записан студент
        /// </summary>
        public Guid CourseId { get; set; }

        /// <summary>
        /// Курс, на который записан студент
        /// </summary>
        public CourseEntity Course { get; set; } = null!;
    }
}