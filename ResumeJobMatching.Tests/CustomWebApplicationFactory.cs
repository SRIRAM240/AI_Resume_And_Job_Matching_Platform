using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResumeJobMatching.Api.Data;

namespace ResumeJobMatching.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=(local);Database=ResumeJobMatchingTestDb;Trusted_Connection=True;");

        builder.UseSetting(
            "Jwt:Key",
            "test-only-signing-key-with-at-least-32-characters");

        builder.UseSetting("Jwt:Issuer", "ResumeJobMatching.Tests");
        builder.UseSetting("Jwt:Audience", "ResumeJobMatching.Tests");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("ResumeJobMatchingIntegrationTests"));
        });
    }
}
