using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Services.Resume;

namespace ResumeJobMatching.Tests;

public class ResumeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly string _testDirectory;
    private readonly ResumeService _service;

    public ResumeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"ResumeJobMatchingTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_testDirectory);

        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = _testDirectory
        };

        _service = new ResumeService(
            _context,
            new ResumeTextExtractor(),
            environment);
    }

    [Fact]
    public async Task GetMyResumesAsync_ReturnsOnlyCurrentUsersResumes()
    {
        _context.Resumes.AddRange(
            new Resume
            {
                UserId = 1,
                FileName = "resume1.pdf",
                FilePath = "App_Data/Resumes/resume1.pdf",
                ExtractedText = "C# developer",
                UploadedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new Resume
            {
                UserId = 2,
                FileName = "resume2.pdf",
                FilePath = "App_Data/Resumes/resume2.pdf",
                ExtractedText = "Java developer",
                UploadedAt = DateTime.UtcNow
            });

        await _context.SaveChangesAsync();

        var result = await _service.GetMyResumesAsync(1);

        var resume = Assert.Single(result);
        Assert.Equal("resume1.pdf", resume.FileName);
    }

    [Fact]
    public async Task GetMyResumesAsync_NoResumes_ReturnsEmptyList()
    {
        var result = await _service.GetMyResumesAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetResumeByIdAsync_OwnResume_ReturnsDetails()
    {
        var resume = new Resume
        {
            UserId = 1,
            FileName = "resume.pdf",
            FilePath = "App_Data/Resumes/resume.pdf",
            ExtractedText = "Experienced C# and ASP.NET Core developer"
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.GetResumeByIdAsync(resume.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(resume.Id, result.Id);
        Assert.Equal("resume.pdf", result.FileName);
        Assert.True(result.HasExtractedText);
        Assert.Equal(resume.ExtractedText.Length, result.ExtractedTextLength);
    }

    [Fact]
    public async Task GetResumeByIdAsync_OtherUsersResume_ReturnsNull()
    {
        var resume = new Resume
        {
            UserId = 2,
            FileName = "private.pdf",
            FilePath = "App_Data/Resumes/private.pdf",
            ExtractedText = "Private resume"
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.GetResumeByIdAsync(resume.Id, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetResumeByIdAsync_NonexistentResume_ReturnsNull()
    {
        var result = await _service.GetResumeByIdAsync(999, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetResumeByIdAsync_ResumeWithoutExtractedText_ReturnsEmptyTextDetails()
    {
        var resume = new Resume
        {
            UserId = 1,
            FileName = "resume.pdf",
            FilePath = "App_Data/Resumes/resume.pdf",
            ExtractedText = null
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.GetResumeByIdAsync(resume.Id, 1);

        Assert.NotNull(result);
        Assert.False(result.HasExtractedText);
        Assert.Equal(0, result.ExtractedTextLength);
    }

    [Fact]
    public async Task DownloadAsync_ExistingResume_ReturnsFileContent()
    {
        var storageDirectory = Path.Combine(
            _testDirectory, "App_Data", "Resumes");
        Directory.CreateDirectory(storageDirectory);

        var content = System.Text.Encoding.UTF8.GetBytes("Test resume content");
        var storedFileName = "stored-resume.pdf";
        await File.WriteAllBytesAsync(
            Path.Combine(storageDirectory, storedFileName), content);

        var resume = new Resume
        {
            UserId = 1,
            FileName = "my-resume.pdf",
            FilePath = Path.Combine("App_Data", "Resumes", storedFileName),
            ExtractedText = "Test resume content"
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.DownloadAsync(resume.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(content, result.Value.Content);
        Assert.Equal("my-resume.pdf", result.Value.FileName);
    }

    [Fact]
    public async Task DownloadAsync_NonexistentResume_ReturnsNull()
    {
        var result = await _service.DownloadAsync(999, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAsync_OtherUsersResume_ReturnsNull()
    {
        var resume = new Resume
        {
            UserId = 2,
            FileName = "private.pdf",
            FilePath = Path.Combine("App_Data", "Resumes", "private.pdf")
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.DownloadAsync(resume.Id, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAsync_MissingPhysicalFile_ThrowsFileNotFoundException()
    {
        var resume = new Resume
        {
            UserId = 1,
            FileName = "missing.pdf",
            FilePath = Path.Combine("App_Data", "Resumes", "missing.pdf")
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.DownloadAsync(resume.Id, 1));
    }

    [Fact]
    public async Task DownloadAsync_InvalidStoragePath_ThrowsInvalidOperationException()
    {
        var resume = new Resume
        {
            UserId = 1,
            FileName = "outside.pdf",
            FilePath = Path.Combine(
                "App_Data", "Resumes", "..", "..", "outside.pdf")
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DownloadAsync(resume.Id, 1));
    }

    [Fact]
    public async Task DeleteAsync_ExistingResume_DeletesFileAndDatabaseRecord()
    {
        var storageDirectory = Path.Combine(
            _testDirectory, "App_Data", "Resumes");
        Directory.CreateDirectory(storageDirectory);

        var storedFileName = "resume.pdf";
        var absolutePath = Path.Combine(storageDirectory, storedFileName);
        await File.WriteAllTextAsync(absolutePath, "Test resume");

        var resume = new Resume
        {
            UserId = 1,
            FileName = "resume.pdf",
            FilePath = Path.Combine("App_Data", "Resumes", storedFileName)
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(resume.Id, 1);

        Assert.True(result);
        Assert.False(File.Exists(absolutePath));
        Assert.Null(await _context.Resumes.FindAsync(resume.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonexistentResume_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersResume_ReturnsFalse()
    {
        var resume = new Resume
        {
            UserId = 2,
            FileName = "private.pdf",
            FilePath = Path.Combine("App_Data", "Resumes", "private.pdf")
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(resume.Id, 1);

        Assert.False(result);
        Assert.NotNull(await _context.Resumes.FindAsync(resume.Id));
    }

    [Fact]
    public async Task DeleteAsync_MissingPhysicalFile_DeletesDatabaseRecord()
    {
        var resume = new Resume
        {
            UserId = 1,
            FileName = "missing.pdf",
            FilePath = Path.Combine("App_Data", "Resumes", "missing.pdf")
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(resume.Id, 1);

        Assert.True(result);
        Assert.Null(await _context.Resumes.FindAsync(resume.Id));
    }

    [Fact]
    public async Task DeleteAsync_InvalidStoragePath_ThrowsInvalidOperationException()
    {
        var resume = new Resume
        {
            UserId = 1,
            FileName = "outside.pdf",
            FilePath = Path.Combine(
                "App_Data", "Resumes", "..", "..", "outside.pdf")
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(resume.Id, 1));

        Assert.NotNull(await _context.Resumes.FindAsync(resume.Id));
    }

    [Fact]
    public async Task UploadAsync_EmptyFile_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        var file = new FormFile(stream, 0, 0, "file", "resume.pdf");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadAsync(file, 1));

        Assert.Equal("The uploaded file is empty.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_FileExceedingFiveMB_ThrowsArgumentException()
    {
        var content = new byte[5 * 1024 * 1024 + 1];

        using var stream = new MemoryStream(content);
        var file = new FormFile(
            stream, 0, content.Length, "file", "resume.pdf");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadAsync(file, 1));

        Assert.Equal("File size cannot exceed 5 MB.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_UnsupportedExtension_ThrowsArgumentException()
    {
        var content = System.Text.Encoding.ASCII.GetBytes("test content");

        using var stream = new MemoryStream(content);
        var file = new FormFile(
            stream, 0, content.Length, "file", "resume.txt");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadAsync(file, 1));

        Assert.Equal(
            "Only PDF and DOCX files are allowed.",
            exception.Message);
    }

    [Fact]
    public async Task UploadAsync_InvalidPdfSignature_ThrowsArgumentException()
    {
        var content = System.Text.Encoding.ASCII.GetBytes("NOTPDF");

        using var stream = new MemoryStream(content);
        var file = new FormFile(
            stream, 0, content.Length, "file", "resume.pdf");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadAsync(file, 1));

        Assert.Equal(
            "The uploaded file is not a valid PDF.",
            exception.Message);
    }

    [Fact]
    public async Task UploadAsync_InvalidDocxSignature_ThrowsArgumentException()
    {
        var content = System.Text.Encoding.ASCII.GetBytes("NOTDOCX");

        using var stream = new MemoryStream(content);
        var file = new FormFile(
            stream, 0, content.Length, "file", "resume.docx");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadAsync(file, 1));

        Assert.Equal(
            "The uploaded file is not a valid DOCX document.",
            exception.Message);
    }

    [Fact]
    public async Task UploadAsync_ValidPdf_SavesFileAndDatabaseRecord()
    {
        var content = CreateMinimalPdf();

        using var stream = new MemoryStream(content);
        var file = new FormFile(
            stream, 0, content.Length, "file", "resume.pdf");

        var result = await _service.UploadAsync(file, 1);

        Assert.Equal("resume.pdf", result.FileName);
        Assert.Equal("Resume uploaded successfully.", result.Message);

        var savedResume = await _context.Resumes
            .SingleAsync(resume => resume.Id == result.Id);

        Assert.Equal(1, savedResume.UserId);
        Assert.Equal("resume.pdf", savedResume.FileName);
        Assert.False(string.IsNullOrWhiteSpace(savedResume.FilePath));

        var absolutePath = Path.Combine(
            _testDirectory,
            savedResume.FilePath);

        Assert.True(File.Exists(absolutePath));
        Assert.StartsWith("%PDF-", await File.ReadAllTextAsync(absolutePath));
    }

    [Fact]
    public async Task UploadAsync_InvalidPdfContent_DeletesStoredFileAndDoesNotSaveRecord()
    {
        // This has a valid PDF signature but is not a valid PDF document.
        var content = System.Text.Encoding.ASCII.GetBytes(
            "%PDF-1.7\nThis is not a valid PDF document.");

        using var stream = new MemoryStream(content);
        var file = new FormFile(
            stream, 0, content.Length, "file", "invalid.pdf");

        await Assert.ThrowsAnyAsync<Exception>(
            () => _service.UploadAsync(file, 1));

        Assert.Empty(await _context.Resumes.ToListAsync());

        var storageDirectory = Path.Combine(
            _testDirectory, "App_Data", "Resumes");

        if (Directory.Exists(storageDirectory))
        {
            Assert.Empty(Directory.GetFiles(storageDirectory));
        }
    }

    private static byte[] CreateMinimalPdf()
    {
        var pdf = """
            %PDF-1.4
            1 0 obj
            << /Type /Catalog /Pages 2 0 R >>
            endobj
            2 0 obj
            << /Type /Pages /Kids [3 0 R] /Count 1 >>
            endobj
            3 0 obj
            << /Type /Page /Parent 2 0 R /MediaBox [0 0 300 144] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>
            endobj
            4 0 obj
            << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
            endobj
            5 0 obj
            << /Length 44 >>
            stream
            BT /F1 18 Tf 20 80 Td (Test Resume) Tj ET
            endstream
            endobj
            trailer
            << /Size 6 /Root 1 0 R >>
            %%EOF
            """;

        return System.Text.Encoding.ASCII.GetBytes(pdf);
    }

    public void Dispose()
    {
        _context.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ResumeJobMatching.Tests";

        public IFileProvider WebRootFileProvider { get; set; }
            = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Testing";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; }
            = new NullFileProvider();
    }
}
