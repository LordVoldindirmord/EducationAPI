using Education.Domain.ResponseModel;

namespace Education.Service.Files.Interfaces
{
    /// <summary>
    /// Внутренний сервис для сохранения, чтения и удаления файлов.
    /// <para>
    /// Создает имя файла на основе Guid и возвращает относительный путь для БД.
    /// Проверку прав и изменение сущностей выполняют вызывающие сервисы.
    /// </para>
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Сохранить файл на сервере. Входящий поток освобождает вызывающий код.
        /// Проверяется расширение и размер, но не фактический формат содержимого.
        /// </summary>
        /// <param name="content">Поток содержимого файла с текущей позиции</param>
        /// <param name="originalFileName">Исходное имя, используется только для определения расширения</param>
        /// <param name="cancellationToken">Сигнал отмены операции</param>
        /// <returns>Относительный путь вида Images/{guid}.jpg или Videos/{guid}.mp4</returns>
        Task<InternalResponse<string>> UploadAsync(
            Stream content,
            string originalFileName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Открыть файл для чтения. Полученный поток должен освободить вызывающий код.
        /// </summary>
        /// <param name="relativePath">Относительный путь из БД</param>
        /// <param name="cancellationToken">Сигнал отмены операции</param>
        /// <returns>Поток чтения или ответ с ошибкой, если файл не найден</returns>
        Task<InternalResponse<Stream>> OpenReadAsync(
            string relativePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Удалить файл с диска. Отсутствующий файл считается уже удаленным.
        /// </summary>
        /// <param name="relativePath">Относительный путь из БД</param>
        /// <param name="cancellationToken">Сигнал отмены операции</param>
        /// <returns>Флаг успешности удаления</returns>
        Task<InternalResponse<bool>> DeleteAsync(
            string relativePath,
            CancellationToken cancellationToken = default);
    }
}
