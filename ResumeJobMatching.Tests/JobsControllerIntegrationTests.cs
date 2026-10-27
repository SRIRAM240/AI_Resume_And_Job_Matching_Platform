using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ResumeJobMatching.Tests;

public class JobsControllerIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new CustomWebApplicationFactory();
    }

    private static string CreateUniqueEmail()
    {
        return $"jobs_{Guid.NewGuid():N}@example.com";
    }

    private static object CreateValidJobRequest()
    {
        return new
        {
            Title = "Software Engineer",
            CompanyName = "Example Company",
            Description = "Develop and maintain backend applications.",
            RequiredSkills = "C#, ASP.NET Core, SQL Server",
            Location = "Hyderabad",
            EmploymentType = "Full-time"
        };
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Jobs Test User",
                Email = CreateUniqueEmail(),
                Password = "SecurePassword123!"
            });

        registrationResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await registrationResponse.Content.ReadAsStringAsync());

        var token = document.RootElement
            .GetProperty("token")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/jobs",
            CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithWhitespaceOnlyTitle_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var request = new
        {
            Title = "   ",
            CompanyName = "Example Company",
            Description = "Develop and maintain backend applications.",
            RequiredSkills = "C#, ASP.NET Core",
            Location = "Hyderabad",
            EmploymentType = "Full-time"
        };

        var response = await client.PostAsJsonAsync("/api/jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/jobs",
            CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        Assert.Equal(
            "Software Engineer",
            document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_WithWhitespaceOnlyTitle_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/jobs",
            CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var document = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var jobId = document.RootElement.GetProperty("id").GetInt32();

        var request = new
        {
            Title = "   ",
            CompanyName = "Example Company",
            Description = "Develop and maintain backend applications.",
            RequiredSkills = "C#, ASP.NET Core",
            Location = "Hyderabad",
            EmploymentType = "Full-time"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/jobs/{jobId}",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task GetAll_WithAuthentication_ReturnsOk()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/jobs/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithZeroId_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/jobs/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task GetById_WithExistingJob_ReturnsOk()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/jobs",
            CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var document = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var jobId = document.RootElement.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var result = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Software Engineer",
            result.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_WithExistingJob_ReturnsOk()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/jobs",
            CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var document = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var jobId = document.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            Title = "Senior Software Engineer",
            CompanyName = "Updated Company",
            Description = "Design and maintain scalable backend applications.",
            RequiredSkills = "C#, ASP.NET Core, SQL Server, Azure",
            Location = "Bengaluru",
            EmploymentType = "Full-time"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/jobs/{jobId}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var result = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "Senior Software Engineer",
            result.RootElement.GetProperty("title").GetString());

        Assert.Equal(
            "Updated Company",
            result.RootElement.GetProperty("companyName").GetString());

        Assert.Equal(
            "Bengaluru",
            result.RootElement.GetProperty("location").GetString());
    }

    [Fact]
    public async Task Update_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/jobs/999999",
            new
            {
                Title = "Software Engineer",
                CompanyName = "Example Company",
                Description = "Develop and maintain backend applications.",
                RequiredSkills = "C#, ASP.NET Core",
                Location = "Hyderabad",
                EmploymentType = "Full-time"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingJob_ReturnsNoContent()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/jobs",
            CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var document = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var jobId = document.RootElement.GetProperty("id").GetInt32();

        var response = await client.DeleteAsync($"/api/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.DeleteAsync("/api/jobs/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
public async Task GetAll_WithoutToken_ReturnsUnauthorized()
{
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/api/jobs");

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);
}
}
