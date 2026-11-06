using Education.Domain.DTO.Request.Courses.Lessons;
using Education.Domain.ResponseModel;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// Сервис для управления уроками курса
    /// </summary>
    public interface ILessonService
    {
        /// <summary>
        /// Создать новый урок с видео, текстовым контентом или тестом.
        /// Урок и его контент сохраняются в общей транзакции.
        /// При ошибке сохранения загруженные файлы удаляются.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для создания урока</param>
        /// <param name="userId">Id пользователя, который создает урок</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> CreateLessonAsync(CreateLessonRequest request, Guid userId);
        /// <summary>
        /// Обновить метаданные и контент урока без изменения его типа и модуля.
        /// Если поле контента не передано, сохраняется текущее значение.
        /// Старые файлы удаляются после успешного сохранения новых данных.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для обновления урока</param>
        /// <param name="userId">Id пользователя, который обновляет урок</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> UpdateLessonAsync(UpdateLessonRequest request, Guid userId);
        /// <summary>
        /// Удалить урок вместе с контентом и прогрессом пользователей.
        /// Файлы видео и превью удаляются после успешного удаления данных из БД.
        /// Ошибки удаления файлов записываются в журнал и не отменяют удаление урока.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для удаления урока</param>
        /// <param name="userId">Id пользователя, который удаляет урок</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> DeleteLessonAsync(DeleteLessonRequest request, Guid userId);
    }
}
