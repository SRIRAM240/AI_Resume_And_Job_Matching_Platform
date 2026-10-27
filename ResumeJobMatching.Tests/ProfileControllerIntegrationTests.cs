using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ResumeJobMatching.Tests;

public class ProfileControllerIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new CustomWebApplicationFactory();
    }

    private static string CreateUniqueEmail()
    {
        return $"profile_{Guid.NewGuid():N}@example.com";
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string fullName = "Profile Test User")
    {
        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = fullName,
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
    public async Task GetMyProfile_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMyProfile_WithValidToken_ReturnsProfile()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var profile = document.RootElement;

        Assert.Equal(
            "Profile Test User",
            profile.GetProperty("fullName").GetString());

        Assert.True(
            profile.TryGetProperty("email", out var email));

        Assert.Contains("@example.com", email.GetString());

        Assert.True(
            profile.TryGetProperty("id", out var id));

        Assert.True(id.GetInt32() > 0);
    }

    [Fact]
    public async Task UpdateMyProfile_WithValidName_ReturnsUpdatedProfile()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/profile/me",
            new
            {
                FullName = "Updated Profile User"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var profile = document.RootElement;

        Assert.Equal(
            "Updated Profile User",
            profile.GetProperty("fullName").GetString());

        Assert.Equal(
            "Profile updated successfully.",
            profile.GetProperty("message").GetString());

        // Verify that the change was persisted.
        var getResponse = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var updatedDocument = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "Updated Profile User",
            updatedDocument.RootElement
                .GetProperty("fullName")
                .GetString());
    }

    [Fact]
    public async Task UpdateMyProfile_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/profile/me",
            new
            {
                FullName = "Updated Profile User"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_WithTooShortName_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/profile/me",
            new
            {
                FullName = "A"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
