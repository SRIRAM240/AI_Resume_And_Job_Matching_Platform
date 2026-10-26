using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.Resume;
using ResumeJobMatching.Api.Models;

namespace ResumeJobMatching.Api.Services.Resume;

public class ResumeService
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly ResumeTextExtractor _textExtractor;
    private readonly IWebHostEnvironment _environment;

    public ResumeService(
        ApplicationDbContext context,
        ResumeTextExtractor textExtractor,
        IWebHostEnvironment environment)
    {
        _context = context;
        _textExtractor = textExtractor;
        _environment = environment;
    }

    // Upload a resume, validate it, extract its text,
    // and save its metadata in the database.
    public async Task<ResumeUploadResponse> UploadAsync(
        IFormFile file,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new ArgumentException(
                "The uploaded file is empty.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException(
                "File size cannot exceed 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        if (extension is not ".pdf" and not ".docx")
        {
            throw new ArgumentException(
                "Only PDF and DOCX files are allowed.");
        }

        // Validate the file signature instead of trusting its extension.
        await using (var input = file.OpenReadStream())
        {
            var header = new byte[5];

            var bytesRead = await input.ReadAsync(
                header.AsMemory(0, header.Length),
                cancellationToken);

            if (extension == ".pdf")
            {
                var pdfSignature = "%PDF-";

                var isPdf = bytesRead == 5 &&
                    System.Text.Encoding.ASCII.GetString(header)
                        == pdfSignature;

                if (!isPdf)
                {
                    throw new ArgumentException(
                        "The uploaded file is not a valid PDF.");
                }
            }
            else
            {
                // DOCX files use the ZIP file format.
                var isZip = bytesRead >= 4 &&
                    header[0] == 0x50 &&
                    header[1] == 0x4B &&
                    header[2] is 0x03 or 0x05 or 0x07 &&
                    header[3] is 0x04 or 0x06 or 0x08;

                if (!isZip)
                {
                    throw new ArgumentException(
                        "The uploaded file is not a valid DOCX document.");
                }
            }
        }

        // Store uploaded files outside wwwroot.
        var storageDirectory = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Resumes");

        Directory.CreateDirectory(storageDirectory);

        // Generate a unique storage filename.
        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var absolutePath = Path.Combine(
            storageDirectory,
            storedFileName);

        // Store a relative path in the database.
        var relativePath = Path.Combine(
            "App_Data",
            "Resumes",
            storedFileName);

        var fileSaved = false;

        try
        {
            // Save the uploaded file.
            await using (var output = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await file.CopyToAsync(
                    output,
                    cancellationToken);
            }

            fileSaved = true;

            // Extract text from the PDF or DOCX file.
            var extractedText =
                await _textExtractor.ExtractTextAsync(
                    absolutePath,
                    extension,
                    cancellationToken);

            // Create the resume database record.
            var resume = new ResumeJobMatching.Api.Models.Resume
            {
                UserId = userId,
                FileName = Path.GetFileName(file.FileName),
                FilePath = relativePath,
                ExtractedText = extractedText,
                UploadedAt = DateTime.UtcNow
            };

            _context.Resumes.Add(resume);

            await _context.SaveChangesAsync(
                cancellationToken);

            return new ResumeUploadResponse
            {
                Id = resume.Id,
                FileName = resume.FileName,
                UploadedAt = resume.UploadedAt,
                Message = "Resume uploaded successfully."
            };
        }
        catch
        {
            // Delete the file if extraction or database saving fails.
            if (fileSaved && File.Exists(absolutePath))
            {
                try
                {
                    File.Delete(absolutePath);
                }
                catch (IOException)
                {
                    // Preserve the original exception.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the original exception.
                }
            }

            throw;
        }
    }

    // Get all resumes belonging to the authenticated user.
    public async Task<List<ResumeUploadResponse>> GetMyResumesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Resumes
            .AsNoTracking()
            .Where(resume => resume.UserId == userId)
            .OrderByDescending(resume => resume.UploadedAt)
            .Select(resume => new ResumeUploadResponse
            {
                Id = resume.Id,
                FileName = resume.FileName,
                UploadedAt = resume.UploadedAt,
                Message = "Resume uploaded successfully."
            })
            .ToListAsync(cancellationToken);
    }
    public async Task<ResumeDetailsResponse?> GetResumeByIdAsync(
    int resumeId,
    int userId,
    CancellationToken cancellationToken = default)
{
    return await _context.Resumes
        .AsNoTracking()
        .Where(resume =>
            resume.Id == resumeId &&
            resume.UserId == userId)
        .Select(resume => new ResumeDetailsResponse
        {
            Id = resume.Id,
            FileName = resume.FileName,
            UploadedAt = resume.UploadedAt,
            HasExtractedText = !string.IsNullOrEmpty(
                resume.ExtractedText),
            ExtractedTextLength = resume.ExtractedText == null
                ? 0
                : resume.ExtractedText.Length
        })
        .FirstOrDefaultAsync(cancellationToken);
}
public async Task<(byte[] Content, string FileName)?> DownloadAsync(
    int resumeId,
    int userId,
    CancellationToken cancellationToken = default)
{
    var resume = await _context.Resumes
        .AsNoTracking()
        .FirstOrDefaultAsync(
            r => r.Id == resumeId && r.UserId == userId,
            cancellationToken);

    if (resume is null)
    {
        return null;
    }

    var storageRoot = Path.GetFullPath(
        Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Resumes"));

    var absolutePath = Path.GetFullPath(
        Path.Combine(
            _environment.ContentRootPath,
            resume.FilePath));

    // Ensure the requested path remains inside the resume storage folder.
    var expectedPrefix = storageRoot + Path.DirectorySeparatorChar;

    if (!absolutePath.StartsWith(
        expectedPrefix,
        StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Invalid resume storage path.");
    }

    if (!File.Exists(absolutePath))
    {
        throw new FileNotFoundException(
            "The resume file could not be found.");
    }

    var content = await File.ReadAllBytesAsync(
        absolutePath,
        cancellationToken);

    return (content, resume.FileName);
}
// Delete a resume belonging to the authenticated user.
public async Task<bool> DeleteAsync(
    int resumeId,
    int userId,
    CancellationToken cancellationToken = default)
{
    var resume = await _context.Resumes
        .FirstOrDefaultAsync(
            r => r.Id == resumeId && r.UserId == userId,
            cancellationToken);

    // Return false if the resume doesn't exist
    // or belongs to another user.
    if (resume is null)
    {
        return false;
    }

    var storageRoot = Path.GetFullPath(
        Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Resumes"));

    var absolutePath = Path.GetFullPath(
        Path.Combine(
            _environment.ContentRootPath,
            resume.FilePath));

    // Ensure the file path stays inside the resume storage directory.
    var expectedPrefix =
        storageRoot + Path.DirectorySeparatorChar;

    if (!absolutePath.StartsWith(
        expectedPrefix,
        StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Invalid resume storage path.");
    }

    // Delete the physical file first if it exists.
    if (File.Exists(absolutePath))
    {
        File.Delete(absolutePath);
    }

    // Delete the database record.
    _context.Resumes.Remove(resume);

    await _context.SaveChangesAsync(cancellationToken);

    return true;
}
}