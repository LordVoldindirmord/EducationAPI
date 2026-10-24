using Education.Domain.DTO.Request.Courses.Courses;
using Education.Domain.ResponseModel;
using Education.Domain.Enums;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// Сервис для управления курсами
    /// </summary>
    public interface ICourseService
    {
        /// <summary>
        /// Создать новый курс в указанной категории.
        /// <para>
        /// Доступно для глобальных ролей Admin, Moderator, CourseCreator.
        /// Создатель автоматически становится участником курса с ролью
        /// <see cref="CourseRole.Owner"/>.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для создания курса</param>
        /// <param name="userId">Id пользователя, который создаёт курс</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> CreateCourseAsync(CreateCourseRequest request, Guid userId);

        /// <summary>
        /// Обновить метаданные курса.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для обновления курса</param>
        /// <param name="userId">Id пользователя, который обновляет курс</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> UpdateCourseAsync(UpdateCourseRequest request, Guid userId);

        /// <summary>
        /// Удалить курс.
        /// <para>
        /// Доступно для локальной роли курса Owner.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для удаления курса</param>
        /// <param name="userId">Id пользователя, который удаляет курс</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> DeleteCourseAsync(DeleteCourseRequest request, Guid userId);

        /// <summary>
        /// Изменить статус публикации курса (опубликовать / снять с публикации).
        /// <para>
        /// Доступно для локальной роли курса Owner.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для изменения статуса публикации</param>
        /// <param name="userId">Id пользователя, который меняет статус</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> PublishCourseAsync(PublishCourseRequest request, Guid userId);
    }
}