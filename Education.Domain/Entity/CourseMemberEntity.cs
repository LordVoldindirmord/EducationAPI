using Education.Domain.Enums;

namespace Education.Domain.Entity
{
    /// <summary>
    /// Участник курса — пользователь с ролью управления курсом.
    /// <para>
    /// В отличие от <see cref="EnrollmentEntity"/> (студенты), эта сущность описывает
    /// тех, кто <b>управляет</b> курсом: владельца, менеджеров, учителей.
    /// </para>
    /// <para>
    /// Связь M:N между <see cref="UserEntity"/> и <see cref="CourseEntity"/> с указанием роли.
    /// </para>
    /// <para>
    /// Один пользователь может быть участником нескольких курсов с разными ролями,
    /// и в одном курсе у пользователя может быть только одна роль.
    /// </para>
    /// </summary>
    public class CourseMemberEntity
    {
        /// <summary>
        /// Id записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Роль пользователя в данном курсе
        /// </summary>
        public required CourseRole Role { get; set; }

        /// <summary>
        /// Дата и время добавления пользователя в курс.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime AddedAt { get; set; }


        // ========================= FK =========================

        /// <summary>
        /// Идентификатор курса, к которому относится участник
        /// </summary>
        public Guid CourseId { get; set; }

        /// <summary>
        /// Курс, к которому относится участник
        /// </summary>
        public CourseEntity Course { get; set; } = null!;

        /// <summary>
        /// Идентификатор пользователя, являющегося участником курса
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Пользователь, являющийся участником курса
        /// </summary>
        public UserEntity User { get; set; } = null!;
    }
}
