using Education.Service.Files.Implementations;

namespace UnitTests.Files;

public class FileManagerValidationTests
{
    [Theory]
    [InlineData("relative", 1, 1)]
    [InlineData("", 1, 1)]
    [InlineData("C:/test", 0, 1)]
    [InlineData("C:/test", 1, 0)]
    public void Constructor_RejectsInvalidRootOrLimits(string root, long imageLimit, long videoLimit)
    {
        Action act = () => new FileManager(root, imageLimit, videoLimit);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Upload_UnsupportedFile_DoesNotCreateStorageDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "education-file-unit", Guid.NewGuid().ToString("N"));
        var sut = new FileManager(root);
        using var input = new MemoryStream(new byte[] { 1 });
        (await sut.UploadAsync(input, "script.exe")).StatusCode.Should().Be(StatusCode.BadRequest);
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public async Task Upload_CancellationBeforeWrite_DoesNotCreateStorageDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "education-file-unit", Guid.NewGuid().ToString("N"));
        var sut = new FileManager(root);
        using var input = new MemoryStream(new byte[] { 1 });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.UploadAsync(input, "cover.png", new CancellationToken(true)));
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public async Task Open_ValidButMissingFile_ReturnsNotFound()
    {
        var sut = new FileManager(Path.Combine(Path.GetTempPath(), "education-file-unit", Guid.NewGuid().ToString("N")));
        (await sut.OpenReadAsync($"Images/{Guid.NewGuid():N}.png")).StatusCode.Should().Be(StatusCode.NotFound);
    }
}
