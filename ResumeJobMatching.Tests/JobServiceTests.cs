using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.Jobs;
using ResumeJobMatching.Api.Models.Jobs;
using ResumeJobMatching.Api.Services.Jobs;

namespace ResumeJobMatching.Tests;

public class JobServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CreateJobRequest CreateValidRequest()
    {
        return new CreateJobRequest
        {
            Title = "Software Engineer",
            CompanyName = "Example Company",
            Description = "Develop and maintain backend applications.",
            RequiredSkills = "C#, ASP.NET Core, SQL Server",
            Location = "Hyderabad",
            EmploymentType = "Full-time"
        };
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesJob()
    {
        using var context = CreateDbContext();
        var service = new JobService(context);

        var result = await service.CreateAsync(CreateValidRequest());

        Assert.True(result.Id > 0);
        Assert.Equal("Software Engineer", result.Title);
        Assert.Equal("Example Company", result.CompanyName);
        Assert.Single(await context.Jobs.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ValuesWithSurroundingSpaces_TrimsValues()
    {
        using var context = CreateDbContext();
        var service = new JobService(context);

        var request = CreateValidRequest();
        request.Title = "  Software Engineer  ";
        request.CompanyName = "  Example Company  ";
        request.RequiredSkills = "  C#, SQL Server  ";

        var result = await service.CreateAsync(request);

        Assert.Equal("Software Engineer", result.Title);
        Assert.Equal("Example Company", result.CompanyName);
        Assert.Equal("C#, SQL Server", result.RequiredSkills);
    }

    [Fact]
    public async Task UpdateAsync_ExistingJob_UpdatesValues()
    {
        using var context = CreateDbContext();

        var job = new Job
        {
            Title = "Junior Developer",
            CompanyName = "Example Company",
            Description = "Develop and maintain backend applications.",
            RequiredSkills = "C#",
            Location = "Hyderabad",
            EmploymentType = "Full-time"
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var service = new JobService(context);
        var request = new UpdateJobRequest
        {
            Title = "  Senior Developer  ",
            CompanyName = "  New Company  ",
            Description = "Design and maintain backend applications.",
            RequiredSkills = "C#, ASP.NET Core",
            Location = "Bengaluru",
            EmploymentType = "Contract"
        };

        var result = await service.UpdateAsync(job.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Senior Developer", result.Title);
        Assert.Equal("New Company", result.CompanyName);
        Assert.Equal("C#, ASP.NET Core", result.RequiredSkills);
    }

    [Fact]
    public async Task UpdateAsync_NonexistentJob_ReturnsNull()
    {
        using var context = CreateDbContext();
        var service = new JobService(context);

        var result = await service.UpdateAsync(
            999,
            new UpdateJobRequest
            {
                Title = "Software Engineer",
                CompanyName = "Example Company",
                Description = "Develop and maintain backend applications.",
                RequiredSkills = "C#",
                Location = "Hyderabad",
                EmploymentType = "Full-time"
            });

        Assert.Null(result);
    }

    [Theory]
[InlineData("   ", "Example Company", "Develop and maintain backend applications.", "C#")]
[InlineData("Software Engineer", "   ", "Develop and maintain backend applications.", "C#")]
[InlineData("Software Engineer", "Example Company", "   ", "C#")]
[InlineData("Software Engineer", "Example Company", "Develop and maintain backend applications.", "   ")]
public async Task CreateAsync_BlankRequiredField_ThrowsArgumentException(
    string title,
    string companyName,
    string description,
    string requiredSkills)
{
    using var context = CreateDbContext();
    var service = new JobService(context);

    var request = new CreateJobRequest
    {
        Title = title,
        CompanyName = companyName,
        Description = description,
        RequiredSkills = requiredSkills,
        Location = "Hyderabad",
        EmploymentType = "Full-time"
    };

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.CreateAsync(request));

    Assert.Empty(await context.Jobs.ToListAsync());
}

[Fact]
public async Task UpdateAsync_WhitespaceOnlyTitle_ThrowsArgumentException()
{
    using var context = CreateDbContext();

    var job = new Job
    {
        Title = "Software Engineer",
        CompanyName = "Example Company",
        Description = "Develop and maintain backend applications.",
        RequiredSkills = "C#",
        Location = "Hyderabad",
        EmploymentType = "Full-time"
    };

    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var service = new JobService(context);
    var request = new UpdateJobRequest
    {
        Title = "   ",
        CompanyName = "Example Company",
        Description = "Develop and maintain backend applications.",
        RequiredSkills = "C#",
        Location = "Hyderabad",
        EmploymentType = "Full-time"
    };

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.UpdateAsync(job.Id, request));

    Assert.Equal("Software Engineer", job.Title);
}
}
