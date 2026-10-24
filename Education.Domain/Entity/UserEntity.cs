using Microsoft.AspNetCore.Identity;

namespace Education.Domain.Entity
{
    /// <summary>
    /// Сущность пользователя
    /// </summary>
    public class UserEntity : IdentityUser<Guid>
    {
        /// <summary>
        /// Имя пользователя.
        /// Максимальная длина: 50 символов
        /// </summary>
        public string FirstName { get; set; } = null!;

        /// <summary>
        /// Фамилия пользователя.
        /// Максимальная длина: 50 символов
        /// </summary>
        public string LastName { get; set; } = null!;

        /// <summary>
        /// Дата создания аккаунта.
        /// Default генерируется на стороне БД
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // ========================= FK =========================

        /// <summary>
        /// Курсы, в которых пользователь является участником (Owner, Manager, Teacher)
        /// </summary>
        public ICollection<CourseMemberEntity> CourseMemberships { get; set; } = new List<CourseMemberEntity>();

        /// <summary>
        /// Курсы, на которые пользователь записан как студент
        /// </summary>
        public ICollection<EnrollmentEntity> Enrollments { get; set; } = new List<EnrollmentEntity>();

        /// <summary>
        /// Покупки пользователя
        /// </summary>
        public ICollection<PurchaseEntity> Purchases { get; set; } = new List<PurchaseEntity>();

        /// <summary>
        /// Прогресс пользователя
        /// </summary>
        public ICollection<UserProgressEntity> UserProgresses { get; set; } = new List<UserProgressEntity>();
    }
}