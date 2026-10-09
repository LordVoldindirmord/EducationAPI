using Education.Domain.Enums;

namespace Education.Domain.ResponseModel
{
    /// <summary>
    /// Вспомогательный класс для удобного создания ответов
    /// </summary>
    public static class CreatorResponse
    {
        /// <summary>
        /// Успешный ответ с данными (200 OK)
        /// </summary>
        public static InternalResponse<T> Ok<T>(T data, string? description = null) => new InternalResponse<T>
        {
            StatusCode = StatusCode.OK,
            Data = data,
            Description = description ?? "Операция выполнена успешно",
        };

        /// <summary>
        /// Ресурс успешно создан (201 Created)
        /// </summary>
        public static InternalResponse<T> Created<T>(T data, string? description = null) => new InternalResponse<T>
        {
            StatusCode = StatusCode.Created,
            Data = data,
            Description = description ?? "Ресурс успешно создан",
        };

        /// <summary>
        /// Запрос выполнен, но контента в нем нет (204 No Content)
        /// </summary>
        public static InternalResponse<T> NoContent<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.NoContent,
            Description = description ?? "Ресурс найден, но данные в нем отсутствуют",
            Data = data,
        };

        /// <summary>
        /// Ошибка: неверные данные (400 Bad Request)
        /// </summary>
        public static InternalResponse<T> BadRequest<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.BadRequest,
            Description = description ?? "Входные параметры не корректны",
            Data = data,
        };

        /// <summary>
        /// Ошибка: не авторизован (401 Unauthorized)
        /// </summary>
        public static InternalResponse<T> Unauthorized<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.Unauthorized,
            Description = description ?? "Пользователь не авторизован",
            Data = data,
        };

        /// <summary>
        /// Ошибка: доступ запрещён (403 Forbidden)
        /// </summary>
        public static InternalResponse<T> Forbidden<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.Forbidden,
            Description = description ?? "Доступ запрещён",
            Data = data,
        };

        /// <summary>
        /// Ошибка: ресурс не найден (404 Not Found)
        /// </summary>
        public static InternalResponse<T> NotFound<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.NotFound,
            Description = description ?? "Ресурс не найден",
            Data = data,
        };

        /// <summary>
        /// Ошибка: конфликт данных (409 Conflict)
        /// </summary>
        public static InternalResponse<T> Conflict<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.Conflict,
            Description = description ?? "Конфликт данных",
            Data = data,
        };

        /// <summary>
        /// Ошибка: слишком много запросов (429 Too Many Requests)
        /// </summary>
        public static InternalResponse<T> TooManyRequests<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.TooManyRequests,
            Description = description ?? "Слишком много запросов. Попробуйте позже",
            Data = data,
        };

        /// <summary>
        /// Ошибка: внутренняя ошибка сервера (500 Internal Server Error)
        /// </summary>
        public static InternalResponse<T> InternalError<T>(string? description = null, T? data = default) => new InternalResponse<T>
        {
            StatusCode = StatusCode.InternalServerError,
            Description = description ?? "Внутренняя ошибка сервера",
            Data = data,
        };
    }
}
