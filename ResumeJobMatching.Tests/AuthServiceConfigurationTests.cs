using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.Auth;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Services.Auth;

namespace ResumeJobMatching.Tests;

public class AuthServiceConfigurationTests
{
    private static (AuthService Service, ApplicationDbContext Context)
        CreateService(Dictionary<string, string?>? overrides = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuthConfigTests_{Guid.NewGuid():N}")
            .Options;

        var context = new ApplicationDbContext(options);

        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "ResumeJobMatching.Tests",
            ["Jwt:Audience"] = "ResumeJobMatching.Tests",
            ["Jwt:ExpiresMinutes"] = "60"
        };

        if (overrides is not null)
        {
            foreach (var setting in overrides)
            {
                settings[setting.Key] = setting.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var passwordHasher = new PasswordHasher<User>();

        var service = new AuthService(
            context,
            passwordHasher,
            configuration);

        return (service, context);
    }

    private static RegisterRequest CreateRequest()
    {
        return new RegisterRequest
        {
            FullName = "Test User",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "SecurePassword123!"
        };
    }

    [Theory]
    [InlineData("Jwt:Key")]
    [InlineData("Jwt:Issuer")]
    [InlineData("Jwt:Audience")]
    public async Task RegisterAsync_WhenJwtSettingIsMissing_ThrowsInvalidOperationException(
        string settingName)
    {
        var (service, context) = CreateService(
            new Dictionary<string, string?>
            {
                [settingName] = null
            });

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegisterAsync(CreateRequest()));

            Assert.Equal(
                "JWT configuration is missing. Check your development secrets.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task RegisterAsync_WhenExpirationIsInvalid_ThrowsInvalidOperationException(
        string? expiration)
    {
        var (service, context) = CreateService(
            new Dictionary<string, string?>
            {
                ["Jwt:ExpiresMinutes"] = expiration
            });

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegisterAsync(CreateRequest()));

            Assert.Equal(
                "Jwt:ExpiresMinutes must be a positive integer.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}
