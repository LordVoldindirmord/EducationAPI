using Education.Domain.Entity;
using Education.Domain.Enums;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для проверки доступа пользователя к курсу
    /// <para>
    /// Агрегирует <see cref="ICourseMembershipService"/> (управляющие)
    /// и <see cref="IEnrollmentService"/> (студенты), давая единый ответ
    /// на вопрос: "что пользователь может делать с этим курсом"
    /// </para>
    /// </summary>
    public interface ICourseAccessService
    {
        /// <summary>
        /// Получить тип доступа пользователя к курсу.
        /// <para>
        /// Возвращает <see cref="CourseAccessType.None"/>, если пользователь
        /// не управляющий и не записан на курс.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns>Тип доступа пользователя к курсу</returns>
        Task<CourseAccessType> GetAccessTypeAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, есть ли у пользователя доступ к контенту курса
        /// (управляющий или записанный студент).
        /// <para>
        /// Не учитывает <see cref="CourseEntity.IsPublished"/> — для этого использовать
        /// <see cref="CanViewAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns><c>true</c> — доступ есть; <c>false</c> — нет.</returns>
        Task<bool> HasAccessAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, может ли пользователь смотреть контент курса.
        /// <para>
        /// Учитывает <see cref="CourseEntity.IsPublished"/>:
        /// <list type="bullet">
        ///     <item>Опубликованный курс — видят управляющие и записанные студенты.</item>
        ///     <item>Неопубликованный — видят только управляющие (Owner/Manager/Teacher).</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns><c>true</c> — можно смотреть; <c>false</c> — нельзя.</returns>
        Task<bool> CanViewAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, может ли пользователь управлять курсом
        /// (редактировать метаданные, удалять, публиковать).
        /// <para>
        /// Управляющие — Owner и Manager. Teacher не управляет метаданными.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns><c>true</c> — может управлять; <c>false</c> — нет.</returns>
        Task<bool> CanManageAsync(Guid userId, Guid courseId);
    }
}