using Education.Domain.ResponseModel;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для работы с записями студентов на курсы.
    /// <para>
    /// Не имеет собственного контроллера и не вызывается
    /// из API напрямую. Используется другими сервисами:
    /// <list type="bullet">
    ///     <item><see cref="ICourseAccessService"/> — для проверки «может ли пользователь смотреть курс».</item>
    ///     <item><c>CoursePurchaseService</c> — для создания записи при успешной оплате.</item>
    ///     <item><c>CourseService</c> — для отображения курса записанному студенту.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Отвечает на вопрос: «записан ли пользователь на курс как студент?».
    /// Не проверяет оплату и публикацию курса — это зона вызывающего кода.
    /// </para>
    /// <para>
    /// Управляющие курсом (Owner/Manager/Teacher) — зона
    /// <see cref="ICourseMembershipService"/>. Здесь только студенты.
    /// </para>
    /// </summary>
    public interface IEnrollmentService
    {
        /// <summary>
        /// Проверить, записан ли пользователь на курс.
        /// </summary>
        Task<bool> IsEnrolledAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Записать пользователя на курс.
        /// <para>
        /// Не выполняет проверок публикации/оплаты — это ответственность вызывающего кода
        /// </para>
        /// </summary>
        Task<InternalResponse<bool>> EnrollAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Отписать пользователя от курса.
        /// </summary>
        Task<InternalResponse<bool>> UnenrollAsync(Guid userId, Guid courseId);
    }
}