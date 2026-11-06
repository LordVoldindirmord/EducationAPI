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

        /// <summary>
        /// Переупаковать ответ с другим типом данных (Data будет null/default).
        /// Используется, когда нужно вернуть ошибку из метода, который ожидает другой тип.
        /// </summary>
        /// <typeparam name="TNew">Новый тип данных</typeparam>
        /// <returns>Новый InternalResponse с тем же статусом и описанием, но с Data = default</returns>
        public InternalResponse<TNew> Repack<TNew>()
        {
            return new InternalResponse<TNew>
            {
                StatusCode = this.StatusCode,
                Description = this.Description,
                Data = default
            };
        }
    }
}