using Education.Domain.ResponseModel;
using Education.Service.Files.Interfaces;

namespace Education.Service.Files.Implementations
{
    public sealed class FileManager : IFileManager
    {
        private readonly string _rootPath;
        public const long DEFAULT_MAX_IMAGE_SIZE = 10 * 1024 * 1024;
        public const long DEFAULT_MAX_VIDEO_SIZE = 1024L * 1024 * 1024;

        private readonly long _maxImageSize;
        private readonly long _maxVideoSize;

        /// <summary>Подготовить файловый менеджер с ограничениями размера.</summary>
        /// <param name="rootPath">Абсолютный путь к папке, где будут храниться файлы, передается при настройке приложения</param>
        /// <param name="maxImageSize">Лимит изображения в байтах, по умолчанию 10 МБ</param>
        /// <param name="maxVideoSize">Лимит видео в байтах, по умолчанию 1 ГБ</param>
        public FileManager(string rootPath, long maxImageSize = DEFAULT_MAX_IMAGE_SIZE,
            long maxVideoSize = DEFAULT_MAX_VIDEO_SIZE)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxImageSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxVideoSize);

            if (!Path.IsPathFullyQualified(rootPath))
                throw new ArgumentException("Требуется абсолютный путь к корневой папке", nameof(rootPath));

            _rootPath = Path.GetFullPath(rootPath);
            _maxImageSize = maxImageSize;
            _maxVideoSize = maxVideoSize;
        }

        /// <inheritdoc />
        public async Task<InternalResponse<string>> UploadAsync(Stream content, string originalFileName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (content == null || !content.CanRead || string.IsNullOrWhiteSpace(originalFileName))
                return CreatorResponse.BadRequest<string>("Не указан файл или поток недоступен для чтения");

            // Используем только расширение. Исходное имя не попадет в путь на сервере.
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            var folder = GetFolder(extension);

            if (folder == null)
                return CreatorResponse.BadRequest<string>("Допустимы JPG, JPEG, PNG, WEBP и MP4");

            var relativePath = $"{folder}/{Guid.NewGuid():N}{extension}";

            var fullPath = GetFullPath(relativePath);

            var maxSize = folder == "Images" ? _maxImageSize : _maxVideoSize;
            bool created = false;
            bool completed = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                // CreateNew запрещает перезапись. await using закроет файл и при ошибке.
                await using (var output = new FileStream(fullPath, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                {
                    created = true;
                    var buffer = new byte[81920];
                    long totalSize = 0;
                    int bytesRead;

                    // Читаем порцию из входящего потока и сразу записываем на диск.
                    while ((bytesRead = await content.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                    {
                        if (bytesRead > maxSize - totalSize)
                            return CreatorResponse.BadRequest<string>("Превышен допустимый размер файла");
                        await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        totalSize += bytesRead;
                    }

                    if (totalSize == 0)
                        return CreatorResponse.BadRequest<string>("Файл пустой");

                    await output.FlushAsync(cancellationToken);
                }

                completed = true;

                return CreatorResponse.Created(relativePath, "Файл сохранен");
            }
            catch (IOException)
            {
                return CreatorResponse.InternalError<string>("Не удалось сохранить файл");
            }
            catch (UnauthorizedAccessException)
            {
                return CreatorResponse.InternalError<string>("Нет доступа к папке файлов");
            }
            finally
            {
                // Убираем только созданный нами незавершенный файл, в том числе при отмене.
                if (created && !completed) File.Delete(fullPath);
            }
        }

        /// <inheritdoc />
        public Task<InternalResponse<Stream>> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var path = GetFullPath(relativePath);
                // Поток остается открытым для вызывающего сервиса.
                Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 81920, FileOptions.Asynchronous);
                return Task.FromResult(CreatorResponse.Ok(stream));
            }
            catch (ArgumentException)
            {
                return Task.FromResult(CreatorResponse.BadRequest<Stream>("Недопустимый путь файла"));
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult(CreatorResponse.NotFound<Stream>("Файл не найден"));
            }
            catch (DirectoryNotFoundException)
            {
                return Task.FromResult(CreatorResponse.NotFound<Stream>("Файл не найден"));
            }
            catch (IOException)
            {
                return Task.FromResult(CreatorResponse.InternalError<Stream>("Не удалось открыть файл"));
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult(CreatorResponse.InternalError<Stream>("Нет доступа к файлу"));
            }
        }

        /// <inheritdoc />
        public Task<InternalResponse<bool>> DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(GetFullPath(relativePath));
                return Task.FromResult(CreatorResponse.Ok(true, "Файл удален"));
            }
            catch (DirectoryNotFoundException)
            {
                return Task.FromResult(CreatorResponse.Ok(true));
            }
            catch (ArgumentException)
            {
                return Task.FromResult(CreatorResponse.BadRequest<bool>("Недопустимый путь файла"));
            }
            catch (IOException)
            {
                return Task.FromResult(CreatorResponse.InternalError<bool>("Не удалось удалить файл"));
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult(CreatorResponse.InternalError<bool>("Нет доступа к файлу"));
            }
        }

        private static string? GetFolder(string extension) => extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" => "Images",
            ".mp4" => "Videos",
            _ => null
        };

        private string GetFullPath(string relativePath)
        {
            // Принимаем только пути, которые может вернуть UploadAsync:
            // разрешенная папка / Guid из 32 символов + разрешенное расширение.
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            var parts = relativePath.Split('/');

            if (parts.Length != 2 || parts[1].Contains('\\') || parts[1].Contains(':'))
                throw new ArgumentException("Недопустимый путь файла");

            var extension = Path.GetExtension(parts[1]);

            if (GetFolder(extension) != parts[0] || GetFolder(extension) == null
                || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(parts[1]), "N", out _))
                throw new ArgumentException("Недопустимый путь файла");

            var path = Path.Combine(_rootPath, parts[0], parts[1]);

            // Даже корректный относительный путь не должен проходить через ссылку на другую папку.
            for (string? current = path; current != null; current = Path.GetDirectoryName(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new ArgumentException("Ссылки в пути файла не поддерживаются");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }

            return path;
        }
    }
}
