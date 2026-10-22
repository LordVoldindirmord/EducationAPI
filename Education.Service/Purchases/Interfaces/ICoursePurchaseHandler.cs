using Education.Domain.Entity;
using Education.Domain.ResponseModel;

namespace Education.Service.Purchases.Interfaces
{
    /// <summary>
    /// Обработчик покупки курса
    /// <para>
    /// Отвечает за валидацию перед покупкой и за выдачу/отзыв доступа к курсу
    /// </para>
    /// </summary>
    public interface ICoursePurchaseHandler
    {
        /// <summary>
        /// Получить курс для его покупки. Если купить курс нельзя, то вернется InternalResponse с ошибкой
        /// <para>
        /// Если где-то по ходу с курсом была ошибка, то вернется в InternalResponse, что именно пошло не так
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns>Объект курса из БД или сообщение об ошибке</returns>
        Task<InternalResponse<CourseEntity>> GetCourseForPurchaseAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Подтвердить покупку в БД
        /// </summary>
        /// <param name="purchase">Сущность покупки</param>
        /// <returns>Флаг успешности операции, true - все хорошо, false - что-то пошло не так</returns>
        Task<InternalResponse<bool>> CompleteCoursePurchaseAsync(PurchaseEntity purchase);

        /// <summary>
        /// Удалить доступ пользователя к курсу
        /// </summary>
        /// <param name="purchase">Сущность покупки</param>
        /// <returns>Флаг успешности операции, true - все хорошо, false - что-то пошло не так</returns>
        Task<InternalResponse<bool>> RevokeCourseAccessAsync(PurchaseEntity purchase);
    }
}
