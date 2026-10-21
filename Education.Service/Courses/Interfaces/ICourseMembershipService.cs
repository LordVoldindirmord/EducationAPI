using Education.Domain.Enums;
using Education.Service.Common.Interfaces;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для работы с участниками курса.
    /// <para>
    /// Отвечает на вопрос: "кем является пользователь в данном курсе?"
    /// Не проверяет активность аккаунта и глобальные роли платформы —
    /// это зона <see cref="IPermissionService"/>. Используется сервисами курсов,
    /// модулей и уроков для проверки прав на управление конкретным курсом.
    /// </para>
    /// </summary>
    public interface ICourseMembershipService
    {
        /// <summary>
        /// Получить роль пользователя в указанном курсе.
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns>
        /// Роль пользователя в курсе (<see cref="CourseRole.Owner"/>,
        /// <see cref="CourseRole.Manager"/> или <see cref="CourseRole.Teacher"/>).
        /// <c>null</c> — если пользователь не является участником курса.
        /// </returns>
        Task<CourseRole?> GetRoleAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, является ли пользователь участником курса
        /// (владельцем, менеджером или учителем).
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns>
        /// <c>true</c> — пользователь есть в списке участников курса
        /// (роль не важна); <c>false</c> — не является участником.
        /// </returns>
        Task<bool> IsMemberAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, что роль пользователя в курсе входит в допустимый набор.
        /// <para>
        /// Удобно для проверок вида «может ли пользователь редактировать контент»:
        /// <c>HasAnyRoleAsync(userId, courseId, [Owner, Manager])</c>.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <param name="roles">Список ролей, любой из которых достаточно для доступа</param>
        /// <returns>
        /// <c>true</c> — пользователь является участником курса и его роль
        /// входит в <paramref name="roles"/>; <c>false</c> — иначе
        /// (в том числе если пользователь не участник курса).
        /// </returns>
        Task<bool> HasAnyRoleAsync(Guid userId, Guid courseId, IEnumerable<CourseRole> roles);
    }
}