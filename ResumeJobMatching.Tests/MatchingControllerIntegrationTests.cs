using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Models.Jobs;

namespace ResumeJobMatching.Tests;

public class MatchingControllerIntegrationTests
{
    private static string CreateUniqueEmail()
    {
        return $"matching_{Guid.NewGuid():N}@example.com";
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Matching Test User",
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

    private static async Task<int> GetUserIdAsync(
        CustomWebApplicationFactory factory,
        string email)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var user = db.Users.First(u => u.Email == email);

        return await Task.FromResult(user.Id);
    }

    private static async Task<(int UserId, string Email)> CreateAuthenticatedUserAsync(
        CustomWebApplicationFactory factory,
        HttpClient client)
    {
        var email = CreateUniqueEmail();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Matching Test User",
                Email = email,
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

        var userId = await GetUserIdAsync(factory, email);

        return (userId, email);
    }

    private static async Task<int> SeedResumeAsync(
        CustomWebApplicationFactory factory,
        int userId,
        string? extractedText)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var resume = new Resume
        {
            UserId = userId,
            FileName = "test-resume.pdf",
            FilePath = "test-resume.pdf",
            ExtractedText = extractedText,
            UploadedAt = DateTime.UtcNow
        };

        db.Resumes.Add(resume);
        await db.SaveChangesAsync();

        return resume.Id;
    }

    private static async Task<int> SeedJobAsync(
        CustomWebApplicationFactory factory,
        string requiredSkills = "C#, ASP.NET Core, SQL Server")
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var job = new Job
        {
            Title = "Backend Developer",
            CompanyName = "Test Company",
            Description = "Develop backend APIs using C# and ASP.NET Core.",
            RequiredSkills = requiredSkills,
            Location = "Remote",
            EmploymentType = "Full-time",
            CreatedAt = DateTime.UtcNow
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        return job.Id;
    }

    [Fact]
    public async Task MatchResume_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/matching",
            new { ResumeId = 1, JobId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MatchResume_WithMissingResume_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/matching",
            new { ResumeId = 9999, JobId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MatchResume_WithMissingJob_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var (userId, _) = await CreateAuthenticatedUserAsync(
            factory, client);

        var resumeId = await SeedResumeAsync(
            factory,
            userId,
            "C# ASP.NET Core SQL Server");

        var response = await client.PostAsJsonAsync(
            "/api/matching",
            new { ResumeId = resumeId, JobId = 9999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MatchResume_WithValidResumeAndJob_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var (userId, _) = await CreateAuthenticatedUserAsync(
            factory, client);

        var resumeId = await SeedResumeAsync(
            factory,
            userId,
            "Experienced developer with C#, ASP.NET Core, SQL Server, Git and Docker.");

        var jobId = await SeedJobAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/matching",
            new { ResumeId = resumeId, JobId = jobId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var result = document.RootElement;

        Assert.Equal(
            resumeId,
            result.GetProperty("resumeId").GetInt32());

        Assert.Equal(
            jobId,
            result.GetProperty("jobId").GetInt32());

        Assert.True(
            result.GetProperty("matchScore").GetDouble() >= 0);
    }

    [Fact]
    public async Task MatchResume_WithEmptyExtractedText_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var (userId, _) = await CreateAuthenticatedUserAsync(
            factory, client);

        var resumeId = await SeedResumeAsync(
            factory,
            userId,
            "   ");

        var jobId = await SeedJobAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/matching",
            new { ResumeId = resumeId, JobId = jobId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MatchAllJobs_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/matching/resume/1/jobs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MatchAllJobs_WithMissingResume_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync(
            "/api/matching/resume/9999/jobs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MatchAllJobs_WithEmptyExtractedText_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var (userId, _) = await CreateAuthenticatedUserAsync(
            factory, client);

        var resumeId = await SeedResumeAsync(
            factory,
            userId,
            null);

        var response = await client.GetAsync(
            $"/api/matching/resume/{resumeId}/jobs");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MatchAllJobs_WithValidResume_ReturnsOkWithResults()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var (userId, _) = await CreateAuthenticatedUserAsync(
            factory, client);

        var resumeId = await SeedResumeAsync(
            factory,
            userId,
            "C# ASP.NET Core SQL Server Git Docker");

        await SeedJobAsync(
            factory,
            "C#, ASP.NET Core");

        await SeedJobAsync(
            factory,
            "SQL Server, Git");

        var response = await client.GetAsync(
            $"/api/matching/resume/{resumeId}/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            JsonValueKind.Array,
            document.RootElement.ValueKind);

        Assert.Equal(2, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task MatchAllJobs_WithNoJobs_ReturnsEmptyArray()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var (userId, _) = await CreateAuthenticatedUserAsync(
            factory, client);

        var resumeId = await SeedResumeAsync(
            factory,
            userId,
            "C# ASP.NET Core");

        var response = await client.GetAsync(
            $"/api/matching/resume/{resumeId}/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            JsonValueKind.Array,
            document.RootElement.ValueKind);

        Assert.Empty(document.RootElement.EnumerateArray());
    }
}
