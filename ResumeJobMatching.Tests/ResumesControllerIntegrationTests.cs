using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ResumeJobMatching.Tests;

public class ResumesControllerIntegrationTests
{
    private static async Task<int> CreateResumeAsync(
    CustomWebApplicationFactory factory,
    int userId,
    string fileName,
    string filePath)
{
    using var scope = factory.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var resume = new Resume
    {
        UserId = userId,
        FileName = fileName,
        FilePath = filePath,
        ExtractedText = "Integration test resume text",
        UploadedAt = DateTime.UtcNow
    };

    db.Resumes.Add(resume);
    await db.SaveChangesAsync();

    return resume.Id;
}
    private static string CreateUniqueEmail()
    {
        return $"resumes_{Guid.NewGuid():N}@example.com";
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Resume Test User",
                Email = CreateUniqueEmail(),
                Password = "SecurePassword123!"
            });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var token = document.RootElement
            .GetProperty("token")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<int> AuthenticateAndGetUserIdAsync(
    HttpClient client)
{
    var response = await client.PostAsJsonAsync(
        "/api/auth/register",
        new
        {
            FullName = "Resume Test User",
            Email = CreateUniqueEmail(),
            Password = "SecurePassword123!"
        });

    response.EnsureSuccessStatusCode();

    using var document = JsonDocument.Parse(
        await response.Content.ReadAsStringAsync());

    var root = document.RootElement;

    var userId = root.GetProperty("userId").GetInt32();
    var token = root.GetProperty("token").GetString();

    Assert.False(string.IsNullOrWhiteSpace(token));

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    return userId;
}

[Fact]
public async Task GetResumeById_WhenResumeBelongsToAnotherUser_ReturnsNotFound()
{
    using var factory = new CustomWebApplicationFactory();

    using var ownerClient = factory.CreateClient();
    using var otherClient = factory.CreateClient();

    var ownerId = await AuthenticateAndGetUserIdAsync(ownerClient);
    await AuthenticateAndGetUserIdAsync(otherClient);

    var resumeId = await CreateResumeAsync(
        factory,
        ownerId,
        "private-resume.pdf",
        Path.Combine("App_Data", "Resumes", "private-resume.pdf"));

    var response = await otherClient.GetAsync(
        $"/api/resumes/{resumeId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

    [Fact]
    public async Task GetMyResumes_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/resumes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResumes_WithAuthentication_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/resumes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        Assert.Equal(
            JsonValueKind.Array,
            document.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetResumeById_WithZeroId_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/resumes/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetResumeById_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/resumes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadResume_WithZeroId_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync(
            "/api/resumes/0/download");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadResume_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync(
            "/api/resumes/999999/download");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteResume_WithZeroId_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.DeleteAsync("/api/resumes/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteResume_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.DeleteAsync("/api/resumes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadResume_WithUnsupportedExtension_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        using var multipart = new MultipartFormDataContent();

        using var fileContent = new ByteArrayContent(
            System.Text.Encoding.UTF8.GetBytes("not a supported resume"));

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("text/plain");

        multipart.Add(fileContent, "File", "resume.txt");

        var response = await client.PostAsync(
            "/api/resumes/upload",
            multipart);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadResume_WithInvalidPdfSignature_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        using var multipart = new MultipartFormDataContent();

        using var fileContent = new ByteArrayContent(
            System.Text.Encoding.UTF8.GetBytes("this is not a PDF"));

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/pdf");

        multipart.Add(fileContent, "File", "resume.pdf");

        var response = await client.PostAsync(
            "/api/resumes/upload",
            multipart);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
public async Task DownloadResume_WhenResumeBelongsToAnotherUser_ReturnsNotFound()
{
    using var factory = new CustomWebApplicationFactory();

    using var ownerClient = factory.CreateClient();
    using var otherClient = factory.CreateClient();

    var ownerId = await AuthenticateAndGetUserIdAsync(ownerClient);
    await AuthenticateAndGetUserIdAsync(otherClient);

    var resumeId = await CreateResumeAsync(
        factory,
        ownerId,
        "private-resume.pdf",
        Path.Combine("App_Data", "Resumes", "private-resume.pdf"));

    var response = await otherClient.GetAsync(
        $"/api/resumes/{resumeId}/download");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task DeleteResume_WhenResumeBelongsToAnotherUser_ReturnsNotFound()
{
    using var factory = new CustomWebApplicationFactory();
    using var ownerClient = factory.CreateClient();
    using var otherClient = factory.CreateClient();

    var ownerId = await AuthenticateAndGetUserIdAsync(ownerClient);
    await AuthenticateAndGetUserIdAsync(otherClient);

    var resumeId = await CreateResumeAsync(
        factory,
        ownerId,
        "private-resume.pdf",
        Path.Combine("App_Data", "Resumes", "private-resume.pdf"));

    var response = await otherClient.DeleteAsync($"/api/resumes/{resumeId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task DownloadResume_WhenPhysicalFileIsMissing_ReturnsNotFound()
{
    using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();

    var userId = await AuthenticateAndGetUserIdAsync(client);

    var missingFilePath = Path.Combine(
        "App_Data",
        "Resumes",
        $"{Guid.NewGuid():N}.pdf");

    var resumeId = await CreateResumeAsync(
        factory,
        userId,
        "missing-resume.pdf",
        missingFilePath);

    var response = await client.GetAsync(
        $"/api/resumes/{resumeId}/download");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
[Fact]
public async Task DeleteResume_WhenResumeExists_DeletesRecordAndPhysicalFile()
{
    using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();

    var userId = await AuthenticateAndGetUserIdAsync(client);

    var scope = factory.Services.CreateScope();
    var environment = scope.ServiceProvider
        .GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

    var fileName = $"{Guid.NewGuid():N}.pdf";
    var relativePath = Path.Combine("App_Data", "Resumes", fileName);
    var absolutePath = Path.Combine(environment.ContentRootPath, relativePath);

    Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
    await File.WriteAllBytesAsync(absolutePath, new byte[] { 1, 2, 3 });

    try
    {
        var resumeId = await CreateResumeAsync(
            factory, userId, fileName, relativePath);

        var deleteResponse = await client.DeleteAsync(
            $"/api/resumes/{resumeId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.False(File.Exists(absolutePath));

        var getResponse = await client.GetAsync(
            $"/api/resumes/{resumeId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    finally
    {
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);

        }
    }
}
[Fact]
public async Task UploadResume_WithoutFile_ReturnsBadRequest()
{
    using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();

    await AuthenticateAsync(client);

    using var multipart = new MultipartFormDataContent();

    var response = await client.PostAsync(
        "/api/resumes/upload",
        multipart);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task UploadResume_WithEmptyPdf_ReturnsBadRequest()
{
    using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();

    await AuthenticateAsync(client);

    using var multipart = new MultipartFormDataContent();
    using var fileContent = new ByteArrayContent(Array.Empty<byte>());

    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue("application/pdf");

    multipart.Add(fileContent, "File", "empty.pdf");

    var response = await client.PostAsync(
        "/api/resumes/upload",
        multipart);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task UploadResume_WithFileExceedingSizeLimit_ReturnsBadRequest()
{
    using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();

    await AuthenticateAsync(client);

    // The service's maximum file size is 5 MB.
    var oversizedContent = new byte[5 * 1024 * 1024 + 1];

    // Include the PDF signature so the size validation is the relevant check.
    oversizedContent[0] = (byte)'%';
    oversizedContent[1] = (byte)'P';
    oversizedContent[2] = (byte)'D';
    oversizedContent[3] = (byte)'F';
    oversizedContent[4] = (byte)'-';

    using var multipart = new MultipartFormDataContent();
    using var fileContent = new ByteArrayContent(oversizedContent);

    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue("application/pdf");

    multipart.Add(fileContent, "File", "oversized.pdf");

    var response = await client.PostAsync(
        "/api/resumes/upload",
        multipart);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task UploadResume_WithValidPdf_ReturnsCreatedAndSavesResume()
{
    // Arrange
    using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();

    var userId = await AuthenticateAndGetUserIdAsync(client);

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

    var pdfBytes = System.Text.Encoding.ASCII.GetBytes(pdf);

    using var multipart = new MultipartFormDataContent();
    using var fileContent = new ByteArrayContent(pdfBytes);

    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue("application/pdf");

    multipart.Add(fileContent, "File", "integration-test-resume.pdf");

    // Act
    var response = await client.PostAsync(
        "/api/resumes/upload",
        multipart);

    // Assert: HTTP response
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var responseBody = await response.Content.ReadAsStringAsync();

    using var document = JsonDocument.Parse(responseBody);
    var responseJson = document.RootElement;

    var resumeId = responseJson.GetProperty("id").GetInt32();

    Assert.True(resumeId > 0);

    Assert.Equal(
        "integration-test-resume.pdf",
        responseJson.GetProperty("fileName").GetString());

    Assert.Equal(
        "Resume uploaded successfully.",
        responseJson.GetProperty("message").GetString());

    Assert.Contains(
        $"/api/resumes/{resumeId}",
        response.Headers.Location?.ToString() ?? string.Empty);

    // Assert: database record
    using var scope = factory.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var savedResume = await db.Resumes
        .SingleAsync(resume => resume.Id == resumeId);

    Assert.Equal(userId, savedResume.UserId);
    Assert.Equal("integration-test-resume.pdf", savedResume.FileName);
    Assert.False(string.IsNullOrWhiteSpace(savedResume.FilePath));

    // Assert: physical file
    var absolutePath = Path.Combine(
        factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()
            .ContentRootPath,
        savedResume.FilePath);

    Assert.True(File.Exists(absolutePath));

    // Assert: resume is visible through the list endpoint
    var listResponse = await client.GetAsync("/api/resumes");

    Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

    var resumes = await listResponse.Content
        .ReadFromJsonAsync<JsonElement[]>();

    Assert.NotNull(resumes);
    Assert.Contains(
        resumes,
        resume => resume.GetProperty("id").GetInt32() == resumeId);
}

}
