using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ResumeJobMatching.Tests;

public class AuthIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new CustomWebApplicationFactory();
    }

    private static string CreateUniqueEmail()
    {
        return $"test_{Guid.NewGuid():N}@example.com";
    }

    private static object CreateRegistrationRequest(
        string fullName = "Avinash Kanaujia",
        string email = "",
        string password = "SecurePassword123!")
    {
        return new
        {
            FullName = fullName,
            Email = string.IsNullOrWhiteSpace(email)
                ? CreateUniqueEmail()
                : email,
            Password = password
        };
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var email = CreateUniqueEmail();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(email: email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        Assert.True(
            document.RootElement.TryGetProperty("token", out var token));

        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
        Assert.Contains(email, body);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var email = CreateUniqueEmail();

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(email: email));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(
                fullName: "Another User",
                email: email));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var email = CreateUniqueEmail();

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(email: email));

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email.ToUpperInvariant(),
                Password = "SecurePassword123!"
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        Assert.True(
            document.RootElement.TryGetProperty("token", out var token));

        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var email = CreateUniqueEmail();

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(email: email));

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = "WrongPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = CreateUniqueEmail(),
                Password = "SecurePassword123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(
                email: "not-an-email"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegistrationRequest(
                email: CreateUniqueEmail(),
                password: "123"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
