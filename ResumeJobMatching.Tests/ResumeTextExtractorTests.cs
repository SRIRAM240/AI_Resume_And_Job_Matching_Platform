using ResumeJobMatching.Api.Services.Resume;

namespace ResumeJobMatching.Tests;

public class ResumeTextExtractorTests
{
    private readonly ResumeTextExtractor _extractor = new();

    [Fact]
    public async Task ExtractTextAsync_UnsupportedExtension_ThrowsInvalidOperationException()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(filePath, "test content");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _extractor.ExtractTextAsync(filePath, ".txt"));

            Assert.Equal(
                "Only PDF and DOCX files are supported.",
                exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _extractor.ExtractTextAsync(
                "unused.pdf",
                ".pdf",
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ExtractTextAsync_MissingPdf_ThrowsFileNotFoundException()
    {
        var missingFile = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.pdf");

        await Assert.ThrowsAnyAsync<Exception>(
            () => _extractor.ExtractTextAsync(missingFile, ".pdf"));
    }

    [Fact]
    public async Task ExtractTextAsync_InvalidPdf_ThrowsException()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.pdf");

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                "This is not a valid PDF document.");

            await Assert.ThrowsAnyAsync<Exception>(
                () => _extractor.ExtractTextAsync(filePath, ".pdf"));
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
