using Education.Domain.Enums;
using Education.Service.Files.Implementations;

namespace UnitTests.Files
{
    public class FileManagerTests
    {
        private static string NewRoot() => Path.Combine(Path.GetTempPath(), "EducationFileManagerTests", Guid.NewGuid().ToString("N"));

        [Theory]
        [InlineData("photo.JPG", "Images", ".jpg")]
        [InlineData("lesson.mp4", "Videos", ".mp4")]
        public async Task Upload_Read_Delete(string name, string folder, string extension)
        {
            var manager = new FileManager(NewRoot());
            byte[] bytes = [1, 2, 3, 4];
            using var input = new MemoryStream(bytes);
            var saved = await manager.UploadAsync(input, name);
            Assert.True(saved.IsSuccess);
            Assert.True(input.CanRead);
            Assert.StartsWith(folder + "/", saved.Data!);
            Assert.EndsWith(extension, saved.Data!);
            Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(saved.Data), "N", out _));

            var opened = await manager.OpenReadAsync(saved.Data!);
            Assert.True(opened.IsSuccess);
            await using (var stream = opened.Data!)
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                Assert.Equal(bytes, copy.ToArray());
            }
            Assert.True((await manager.DeleteAsync(saved.Data!)).IsSuccess);
            Assert.True((await manager.DeleteAsync(saved.Data!)).IsSuccess);
            Assert.Equal(StatusCode.NotFound, (await manager.OpenReadAsync(saved.Data!)).StatusCode);
        }

        [Fact]
        public async Task InvalidUploads_DoNotLeaveFiles()
        {
            var root = NewRoot();
            var manager = new FileManager(root, maxImageSize: 2);
            using var input = new MemoryStream(new byte[3]);
            Assert.Equal(StatusCode.BadRequest, (await manager.UploadAsync(input, "large.jpg")).StatusCode);
            Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
            input.Position = 0;
            Assert.Equal(StatusCode.BadRequest, (await manager.UploadAsync(input, "script.exe")).StatusCode);
            using var empty = new MemoryStream();
            Assert.Equal(StatusCode.BadRequest, (await manager.UploadAsync(empty, "empty.png")).StatusCode);
            Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        }

        [Theory]
        [InlineData("../secret.txt")]
        [InlineData("Images/../../secret.jpg")]
        [InlineData("C:/secret.jpg")]
        [InlineData("Images/test.jpg")]
        [InlineData("Other/01234567890123456789012345678901.jpg")]
        public async Task InvalidPaths_AreRejected(string path)
        {
            var manager = new FileManager(NewRoot());
            Assert.Equal(StatusCode.BadRequest, (await manager.OpenReadAsync(path)).StatusCode);
            Assert.Equal(StatusCode.BadRequest, (await manager.DeleteAsync(path)).StatusCode);
        }

        [Fact]
        public async Task CancellationDuringUpload_RemovesPartialFile()
        {
            var root = NewRoot();
            var manager = new FileManager(root);
            using var source = new CancellationTokenSource();
            using var input = new CancelingStream(source);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.UploadAsync(input, "image.jpg", source.Token));
            Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        }

        // Имитируем обрыв после начала записи без настоящего сетевого соединения.
        private sealed class CancelingStream(CancellationTokenSource source) : MemoryStream(new byte[10])
        {
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                source.Cancel();
                return base.ReadAsync(buffer, cancellationToken);
            }
        }
    }
}
