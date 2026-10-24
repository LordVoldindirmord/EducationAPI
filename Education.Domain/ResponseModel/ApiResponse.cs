namespace Education.Domain.ResponseModel
{
    public class ApiResponse<T>
    {
        /// <summary>
        /// Описание ошибки или информационное сообщение
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Данные, возвращаемые в ответе (может быть null при ошибке)
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        /// Флаг успешного выполнения операции
        /// </summary>
        public bool IsSuccess { get; init; }
    }
}