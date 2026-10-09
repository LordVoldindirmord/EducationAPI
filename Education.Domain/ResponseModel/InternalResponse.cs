using Education.Domain.Enums;

namespace Education.Domain.ResponseModel
{
    /// <summary>
    /// Универсальный класс-обёртка для ответов от сервисного слоя
    /// Содержит данные, статус и описание ошибки
    /// </summary>
    /// <typeparam name="T">Тип возвращаемых данных</typeparam>
    public class InternalResponse<T> : ApiResponse<T>
    {
        /// <summary>
        /// Код статуса выполнения операции
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Флаг успешного выполнения операции
        /// </summary>
        public new bool IsSuccess =>
            StatusCode == StatusCode.OK ||
            StatusCode == StatusCode.Created ||
            StatusCode == StatusCode.NoContent;

        /// <summary>
        /// Преобразовать к BaseResponse (получить именно новый объект)
        /// </summary>
        /// <returns>Объект класса BaseResponse</returns>
        public ApiResponse<T> GetApiResponse() => new ApiResponse<T>
        {
            Description = Description,
            Data = Data,
            IsSuccess = IsSuccess,
        };
    }
}
