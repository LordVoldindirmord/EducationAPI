using Education.Domain.DTO.Request.Courses.Modules;
using Education.Domain.ResponseModel;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// Сервис для управления модулями курса
    /// </summary>
    public interface IModuleService
    {
        /// <summary>
        /// Создать новый модуль в указанном курсе.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для создания модуля</param>
        /// <param name="userId">Id пользователя, который создает модуль</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> CreateModuleAsync(CreateModuleRequest request, Guid userId);
        /// <summary>
        /// Обновить название, описание и порядковый номер модуля.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для обновления модуля</param>
        /// <param name="userId">Id пользователя, который обновляет модуль</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> UpdateModuleAsync(UpdateModuleRequest request, Guid userId);
        /// <summary>
        /// Удалить модуль вместе с уроками, их контентом и прогрессом пользователей.
        /// Файлы видео и превью удаляются после успешного удаления данных из БД.
        /// Ошибки удаления файлов записываются в журнал и не отменяют удаление модуля.
        /// <para>
        /// Доступно только для локальных ролей курса Owner, Manager.
        /// </para>
        /// </summary>
        /// <param name="request">Данные для удаления модуля</param>
        /// <param name="userId">Id пользователя, который удаляет модуль</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> DeleteModuleAsync(DeleteModuleRequest request, Guid userId);
    }
}
