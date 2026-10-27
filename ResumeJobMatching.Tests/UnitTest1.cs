
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Models.Jobs;
using ResumeJobMatching.Api.Services.Matching;

namespace ResumeJobMatching.Tests;

public class UnitTest1
{
    // -------------------------------
    // Existing SkillMatcher tests
    // -------------------------------

    [Theory]
    [InlineData("Experienced in C Sharp", "C#")]
    [InlineData("Experienced in C#", "C Sharp")]
    public void ContainsSkill_CSharpVariations_ReturnsTrue(
        string resumeText,
        string skill)
    {
        var result = SkillMatcher.ContainsSkill(resumeText, skill);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Developed REST APIs", "RESTful APIs")]
    [InlineData("Developed RESTful APIs", "REST APIs")]
    public void ContainsSkill_RestApiVariations_ReturnsTrue(
        string resumeText,
        string skill)
    {
        var result = SkillMatcher.ContainsSkill(resumeText, skill);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Used Microsoft SQL Server", "SQL Server")]
    [InlineData("Used SQL Server", "Microsoft SQL Server")]
    public void ContainsSkill_SqlServerVariations_ReturnsTrue(
        string resumeText,
        string skill)
    {
        var result = SkillMatcher.ContainsSkill(resumeText, skill);

        Assert.True(result);
    }

    [Fact]
    public void ContainsSkill_GitDoesNotMatchGitHub_ReturnsFalse()
    {
        var result = SkillMatcher.ContainsSkill(
            "My projects are available on GitHub",
            "Git");

        Assert.False(result);
    }

    [Fact]
    public void ContainsSkill_EmptySkill_ReturnsFalse()
    {
        var result = SkillMatcher.ContainsSkill(
            "Experienced in C# and SQL Server",
            "");

        Assert.False(result);
    }

    // -------------------------------
    // Existing Authentication tests
    // -------------------------------

    [Fact]
    public async Task MatchingEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var response = await client.PostAsync(
            "/api/matching",
            new StringContent(
                """{"resumeId": 3, "jobId": 1}""",
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MatchingEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt");

        var response = await client.PostAsync(
            "/api/matching",
            new StringContent(
                """{"resumeId": 3, "jobId": 1}""",
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // -------------------------------
    // Matching service test helpers
    // -------------------------------

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User CreateUser(int id)
    {
        return new User
        {
            Id = id,
            FullName = $"Test User {id}",
            Email = $"user{id}@example.com",
            PasswordHash = "test-hash"
        };
    }

    private static Resume CreateResume(
        int id,
        int userId,
        string? extractedText)
    {
        return new Resume
        {
            Id = id,
            UserId = userId,
            FileName = $"resume{id}.pdf",
            FilePath = $"/test/resume{id}.pdf",
            ExtractedText = extractedText
        };
    }

    private static Job CreateJob(
    int id,
    string requiredSkills,
    string description = "Test job description")
{
    return new Job
    {
        Id = id,
        Title = ".NET Backend Developer",
        CompanyName = "Test Company",
        Description = description,
        RequiredSkills = requiredSkills
    };
}

    // -------------------------------
    // New ResumeMatchingService tests
    // -------------------------------

    [Fact]
    public async Task MatchAsync_CalculatesCorrectScoreAndSkills()
    {
        using var context = CreateDbContext();

        var user = CreateUser(1);

        context.Users.Add(user);

        context.Resumes.Add(
            CreateResume(
                id: 1,
                userId: 1,
                extractedText: "Experienced in C# and REST APIs"));

        context.Jobs.Add(
            CreateJob(
                id: 1,
                requiredSkills: "C#, REST APIs, SQL Server, Git"));

        await context.SaveChangesAsync();

        var service = new ResumeMatchingService(context);

        var result = await service.MatchAsync(
            resumeId: 1,
            jobId: 1,
            userId: 1);

        Assert.NotNull(result);

        Assert.Equal(57.14, result.SkillMatchScore);
Assert.Equal(0.0, result.DescriptionRelevanceScore);
Assert.Equal(40.0, result.MatchScore);

        Assert.Equal(
            new[] { "C#", "REST APIs" },
            result.MatchingSkills);

        Assert.Equal(
            new[] { "SQL Server", "Git" },
            result.MissingSkills);

        Assert.Equal("resume1.pdf", result.ResumeFileName);

        Assert.Equal(".NET Backend Developer", result.JobTitle);

        Assert.Equal("Test Company", result.CompanyName);

        Assert.Equal("Matched 2 of 4 required skills.", result.Summary);
    }


[Fact]
public async Task MatchAsync_DescriptionRelevanceContributesToFinalScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(
            id: 1,
            userId: 1,
            extractedText: "Experienced C# developer with REST APIs and SQL Server experience"));

    context.Jobs.Add(
        CreateJob(
            id: 1,
            requiredSkills: "C#, SQL Server",
            description: "Experienced C# developer with REST APIs and SQL Server experience"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(
        resumeId: 1,
        jobId: 1,
        userId: 1);

    Assert.NotNull(result);

    Assert.Equal(100.0, result.SkillMatchScore);
    Assert.Equal(100.0, result.DescriptionRelevanceScore);
    Assert.Equal(100.0, result.MatchScore);
}


    [Fact]
    public async Task MatchAsync_ResumeDoesNotExist_ReturnsNull()
    {
        using var context = CreateDbContext();

        context.Users.Add(CreateUser(1));

        context.Jobs.Add(CreateJob(1, "C#, SQL Server"));

        await context.SaveChangesAsync();

        var service = new ResumeMatchingService(context);

        var result = await service.MatchAsync(
            resumeId: 999,
            jobId: 1,
            userId: 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task MatchAsync_ResumeBelongsToAnotherUser_ReturnsNull()
    {
        using var context = CreateDbContext();

        context.Users.AddRange(
            CreateUser(1),
            CreateUser(2));

        context.Resumes.Add(
            CreateResume(
                id: 1,
                userId: 2,
                extractedText: "Experienced in C# and SQL Server"));

        context.Jobs.Add(CreateJob(1, "C#, SQL Server"));

        await context.SaveChangesAsync();

        var service = new ResumeMatchingService(context);

        // User 1 attempts to access User 2's resume.
        var result = await service.MatchAsync(
            resumeId: 1,
            jobId: 1,
            userId: 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task MatchAsync_JobDoesNotExist_ReturnsNull()
    {
        using var context = CreateDbContext();

        context.Users.Add(CreateUser(1));

        context.Resumes.Add(
            CreateResume(
                id: 1,
                userId: 1,
                extractedText: "Experienced in C#"));

        await context.SaveChangesAsync();

        var service = new ResumeMatchingService(context);

        var result = await service.MatchAsync(
            resumeId: 1,
            jobId: 999,
            userId: 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task MatchAsync_ResumeHasEmptyExtractedText_ThrowsException()
    {
        using var context = CreateDbContext();

        context.Users.Add(CreateUser(1));

        context.Resumes.Add(
            CreateResume(
                id: 1,
                userId: 1,
                extractedText: "   "));

        context.Jobs.Add(CreateJob(1, "C#, SQL Server"));

        await context.SaveChangesAsync();

        var service = new ResumeMatchingService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MatchAsync(
                resumeId: 1,
                jobId: 1,
                userId: 1));

        Assert.Equal(
            "The selected resume does not contain extracted text.",
            exception.Message);
    }


[Fact]
public void DescriptionRelevanceMatcher_PartialTermsMatch_ReturnsExpectedScore()
{
    var resumeText = "Experienced C# developer with SQL Server knowledge";

    var jobDescription = "C# developer with SQL Server and Docker experience";

    var score = DescriptionRelevanceMatcher.CalculateScore(
        resumeText,
        jobDescription);

    Assert.Equal(72.73, score);
}


[Fact]
public async Task MatchAsync_DuplicateRequiredSkills_DoesNotInflateScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in C#"));

    context.Jobs.Add(
        CreateJob(1, "C#, C#, SQL Server"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(60.0, result.SkillMatchScore);
Assert.Equal(0.0, result.DescriptionRelevanceScore);
Assert.Equal(42.0, result.MatchScore);
    Assert.Equal(new[] { "C#" }, result.MatchingSkills);
    Assert.Equal(new[] { "SQL Server" }, result.MissingSkills);
    Assert.Equal("Matched 1 of 2 required skills.", result.Summary);
}

[Fact]
public async Task MatchAsync_NoRequiredSkills_ReturnsZeroScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in C# and SQL Server"));

    context.Jobs.Add(CreateJob(1, ""));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(0.0, result.MatchScore);
    Assert.Empty(result.MatchingSkills);
    Assert.Empty(result.MissingSkills);
    Assert.Equal(
        "No required skills were specified for this job.",
        result.Summary);
}

[Fact]
public async Task MatchAsync_NoRequiredSkillsWithMatchingDescription_ReturnsZeroScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    const string resumeText =
        "Experienced C# developer with SQL Server experience";

    context.Resumes.Add(
        CreateResume(1, 1, resumeText));

    context.Jobs.Add(
        CreateJob(
            id: 1,
            requiredSkills: "",
            description: resumeText));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(0.0, result.SkillMatchScore);
    Assert.Equal(100.0, result.DescriptionRelevanceScore);
    Assert.Equal(0.0, result.MatchScore);
    Assert.Empty(result.MatchingSkills);
    Assert.Empty(result.MissingSkills);
    Assert.Equal(
        "No required skills were specified for this job.",
        result.Summary);
}


[Fact]
public async Task MatchAsync_NoMatchingSkills_ReturnsZeroScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in Python and Django"));

    context.Jobs.Add(
        CreateJob(1, "C#, SQL Server"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(0.0, result.MatchScore);
    Assert.Empty(result.MatchingSkills);
    Assert.Equal(
        new[] { "C#", "SQL Server" },
        result.MissingSkills);
    Assert.Equal("Matched 0 of 2 required skills.", result.Summary);
}
[Fact]
public void ContainsSkill_SqlDoesNotMatchSqlServer_ReturnsFalse()
{
    var result = SkillMatcher.ContainsSkill(
        "Experienced in SQL Server",
        "SQL");

    Assert.False(result);
}

[Fact]
public void ContainsSkill_AspNetCore_MatchesAspNetVariation()
{
    var result = SkillMatcher.ContainsSkill(
        "Experienced in ASP.NET Core",
        "ASP.NET Core");

    Assert.True(result);
}

[Fact]
public void ContainsSkill_CSharpDoesNotMatchC_ReturnsFalse()
{
    var result = SkillMatcher.ContainsSkill(
        "Experienced in C Sharp",
        "C");

    Assert.False(result);
}

[Fact]
public void ContainsSkill_GitMatchesStandaloneGit_ReturnsTrue()
{
    var result = SkillMatcher.ContainsSkill(
        "Used Git for version control",
        "Git");

    Assert.True(result);
}
[Fact]
public async Task MatchAsync_ReturnsCorrectSkillMatchDetails()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(
            1,
            1,
            "Experienced in C# and REST APIs"));

    context.Jobs.Add(
        CreateJob(
            1,
            "C#, REST APIs, SQL Server"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(3, result.SkillMatchDetails.Count);

    Assert.Collection(
        result.SkillMatchDetails,
        detail =>
        {
            Assert.Equal("C#", detail.Skill);
            Assert.True(detail.IsMatched);
        },
        detail =>
        {
            Assert.Equal("REST APIs", detail.Skill);
            Assert.True(detail.IsMatched);
        },
        detail =>
        {
            Assert.Equal("SQL Server", detail.Skill);
            Assert.False(detail.IsMatched);
        });
}
[Fact]
public async Task MatchAllJobsAsync_ReturnsJobsRankedByMatchScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in C#, REST APIs, and SQL Server"));

    context.Jobs.AddRange(
        CreateJob(1, "Git, Docker"),
        CreateJob(2, "C#, REST APIs, SQL Server"),
        CreateJob(3, "C#, Git"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var results = await service.MatchAllJobsAsync(1, 1);

    Assert.Equal(3, results.Count);

    Assert.Equal(2, results[0].JobId);
    Assert.Equal(100.0, results[0].SkillMatchScore);
Assert.Equal(0.0, results[0].DescriptionRelevanceScore);
Assert.Equal(70.0, results[0].MatchScore);

    Assert.Equal(3, results[1].JobId);
    Assert.Equal(75.0, results[1].SkillMatchScore);
Assert.Equal(0.0, results[1].DescriptionRelevanceScore);
Assert.Equal(52.5, results[1].MatchScore);

    Assert.Equal(1, results[2].JobId);
    Assert.Equal(0.0, results[2].SkillMatchScore);
Assert.Equal(0.0, results[2].DescriptionRelevanceScore);
Assert.Equal(0.0, results[2].MatchScore);
}
[Fact]
public async Task MatchAllJobsAsync_ReturnsNotFoundForAnotherUsersResume()
{
    using var context = CreateDbContext();

    context.Users.AddRange(
        CreateUser(1),
        CreateUser(2));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in C# and SQL Server"));

    context.Jobs.Add(
        CreateJob(1, "C#, SQL Server"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    await Assert.ThrowsAsync<KeyNotFoundException>(
        () => service.MatchAllJobsAsync(1, 2));
}
[Fact]
public async Task MatchAllJobsAsync_ThrowsWhenResumeHasNoExtractedText()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, null));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.MatchAllJobsAsync(1, 1));
}

[Theory]
[InlineData("Developed applications using ASP.NET Core", "ASP.NET Core")]
[InlineData("Developed applications using ASP Net Core", "ASP.NET Core")]
public void ContainsSkill_AspNetCoreVariations_ReturnsTrue(
    string resumeText,
    string skill)
{
    var result = SkillMatcher.ContainsSkill(resumeText, skill);

    Assert.True(result);
}

[Theory]
[InlineData("Used Entity Framework Core for database operations", "Entity Framework Core")]
[InlineData("Used Entity Framework for database operations", "Entity Framework")]
public void ContainsSkill_EntityFrameworkVariations_ReturnsTrue(
    string resumeText,
    string skill)
{
    var result = SkillMatcher.ContainsSkill(resumeText, skill);

    Assert.True(result);
}

[Fact]
public void ContainsSkill_ApiDoesNotMatchRestApis_ReturnsFalse()
{
    var result = SkillMatcher.ContainsSkill(
        "Developed a web API",
        "REST APIs");

    Assert.False(result);
}

[Fact]
public void ContainsSkill_EntityFrameworkDoesNotMatchEntityFrameworkCore_ReturnsFalse()
{
    var result = SkillMatcher.ContainsSkill(
        "Used Entity Framework for database operations",
        "Entity Framework Core");

    Assert.False(result);
}

[Theory]
[InlineData("Experienced in C Sharp", "C#")]
[InlineData("Experienced in ASP Net Core", "ASP.NET Core")]
[InlineData("Developed RESTful APIs", "REST APIs")]
public void ContainsSkill_KnownVariations_ReturnsTrue(
    string resumeText,
    string skill)
{
    Assert.True(SkillMatcher.ContainsSkill(resumeText, skill));
}

[Theory]
[InlineData("Used GitHub for source control", "Git")]
[InlineData("Experienced in SQL", "SQL Server")]
[InlineData("Used Entity Framework for database operations",
            "Entity Framework Core")]
public void ContainsSkill_RelatedButDifferentSkills_ReturnsFalse(
    string resumeText,
    string skill)
{
    Assert.False(SkillMatcher.ContainsSkill(resumeText, skill));
}


[Fact]
public async Task MatchAsync_EmptyJobDescription_UsesOnlySkillScoreWeight()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(
            1,
            1,
            "Experienced in C#"));

    context.Jobs.Add(
        CreateJob(
            id: 1,
            requiredSkills: "C#, Git",
            description: ""));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(75.0, result.SkillMatchScore);
    Assert.Equal(0.0, result.DescriptionRelevanceScore);
    Assert.Equal(52.5, result.MatchScore);
    Assert.Equal(new[] { "C#" }, result.MatchingSkills);
    Assert.Equal(new[] { "Git" }, result.MissingSkills);
}


[Fact]
public async Task MatchAsync_HighWeightMatchedSkill_ProducesWeightedScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in C#"));

    context.Jobs.Add(
        CreateJob(1, "C#, Git"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(75.0, result.SkillMatchScore);
Assert.Equal(0.0, result.DescriptionRelevanceScore);
Assert.Equal(52.5, result.MatchScore);
}

[Fact]
public async Task MatchAsync_LowWeightMatchedSkill_ProducesWeightedScore()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in Git"));

    context.Jobs.Add(
        CreateJob(1, "C#, Git"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(25.0, result.SkillMatchScore);
Assert.Equal(0.0, result.DescriptionRelevanceScore);
Assert.Equal(17.5, result.MatchScore);
}

[Fact]
public async Task MatchAsync_UnlistedSkills_UseDefaultWeight()
{
    using var context = CreateDbContext();

    context.Users.Add(CreateUser(1));

    context.Resumes.Add(
        CreateResume(1, 1, "Experienced in Python"));

    context.Jobs.Add(
        CreateJob(1, "Python, Java"));

    await context.SaveChangesAsync();

    var service = new ResumeMatchingService(context);

    var result = await service.MatchAsync(1, 1, 1);

    Assert.NotNull(result);
    Assert.Equal(50.0, result.SkillMatchScore);
Assert.Equal(0.0, result.DescriptionRelevanceScore);
Assert.Equal(35.0, result.MatchScore);
}

[Fact]
public void DescriptionRelevanceMatcher_AllTermsMatch_Returns100()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "Experienced backend developer using C# and APIs",
        "Backend developer using C# and APIs");

    Assert.Equal(100.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_HalfTermsMatch_Returns50()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "Experienced backend developer",
        "Backend developer using C# and APIs");

    Assert.Equal(25.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_EmptyDescription_ReturnsZero()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "Experienced backend developer",
        "");

    Assert.Equal(0.0, score);
}


[Fact]
public void DescriptionRelevanceMatcher_TechnicalAliasesMatch()
{
    var resumeText = "Experienced in JavaScript, PostgreSQL, and REST APIs";
    var jobDescription = "Experience with JS, Postgres, and RESTful API";

    var score = DescriptionRelevanceMatcher.CalculateScore(
        resumeText,
        jobDescription);

    Assert.Equal(100.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_CaseDifferences_Returns100()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "EXPERIENCED C# DEVELOPER",
        "experienced c# developer");

    Assert.Equal(100.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_EmptyResume_ReturnsZero()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "",
        "C# developer with SQL Server experience");

    Assert.Equal(0.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_DuplicateTermsDoNotInflateScore()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "C# C# C# developer",
        "C# developer");

    Assert.Equal(100.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_UnmatchedTerms_ReturnsZero()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "Experienced Python engineer",
        "C# developer");

    Assert.Equal(0.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_CSharpAliasesMatch()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "Experienced C Sharp developer",
        "Experienced C# developer");

    Assert.Equal(100.0, score);
}

[Fact]
public void DescriptionRelevanceMatcher_AspNetCoreAliasesMatch()
{
    var score = DescriptionRelevanceMatcher.CalculateScore(
        "Experienced in ASP Net Core development",
        "Experienced in ASP.NET Core development");

    Assert.Equal(100.0, score);
}




    // -------------------------------
    // Additional SkillMatcher edge cases
    // -------------------------------

    [Theory]
    [InlineData("Experienced in JavaScript", "Java")]
    [InlineData("My projects are on GitHub", "Git")]
    [InlineData("Used Entity Framework", "Entity Framework Core")]
    [InlineData("Developed a web API", "REST APIs")]
    public void ContainsSkill_UnrelatedOrIncompleteSkill_ReturnsFalse(
        string resumeText,
        string skill)
    {
        Assert.False(SkillMatcher.ContainsSkill(resumeText, skill));
    }

    [Theory]
    [InlineData("Experienced in EF Core", "Entity Framework Core")]
    [InlineData("Used Postgres for database storage", "PostgreSQL")]
    [InlineData("Developed RESTful APIs", "REST APIs")]
    [InlineData("Experienced in ASP Net Core", "ASP.NET Core")]
    public void ContainsSkill_KnownAliases_ReturnsTrue(
        string resumeText,
        string skill)
    {
        Assert.True(SkillMatcher.ContainsSkill(resumeText, skill));
    }

    [Theory]
    [InlineData("", "C#")]
    [InlineData("Experienced in C#", "")]
    [InlineData("Experienced in C#", "   ")]
    public void ContainsSkill_EmptyInput_ReturnsFalse(
        string resumeText,
        string skill)
    {
        Assert.False(SkillMatcher.ContainsSkill(resumeText, skill));
    }


    [Fact]
    public void DescriptionRelevanceMatcher_TechnicalTermsMatchRegardlessOfCase()
    {
        var resumeText =
            "C# ASP.NET CORE SQL SERVER Docker";

        var jobDescription =
            "c# asp.net core sql server docker";

        var score = DescriptionRelevanceMatcher.CalculateScore(
            resumeText,
            jobDescription);

        Assert.Equal(100.0, score);
    }

    [Fact]
    public void DescriptionRelevanceMatcher_PrioritizesTechnicalTerms()
    {
        var resumeText =
            "Experienced developer with C# and SQL Server";

        var jobDescription =
            "Experienced developer with C#, SQL Server, and Docker";

        var score = DescriptionRelevanceMatcher.CalculateScore(
            resumeText,
            jobDescription);

        Assert.True(score > 50.0);
        Assert.True(score < 100.0);
    }

    [Fact]
    public void DescriptionRelevanceMatcher_IgnoresRepeatedResumeTerms()
    {
        var resumeText =
            "C# C# C# developer with SQL Server experience";

        var jobDescription =
            "C# developer with SQL Server experience";

        var score = DescriptionRelevanceMatcher.CalculateScore(
            resumeText,
            jobDescription);

        Assert.Equal(100.0, score);
    }

[Theory]
[InlineData("EXPERIENCED IN SQL SERVER", "SQL Server")]
[InlineData("experienced in docker", "Docker")]
[InlineData("Experienced in C SHARP", "C#")]
public void ContainsSkill_CaseInsensitiveMatching_ReturnsTrue(
    string resumeText,
    string skill)
{
    Assert.True(SkillMatcher.ContainsSkill(resumeText, skill));
}

[Theory]
[InlineData("Experienced in C#", "C#")]
[InlineData("Experienced in C Sharp", "C#")]
[InlineData("Experienced in CSharp", "C#")]
public void ContainsSkill_CSharpFormattingVariations_ReturnsTrue(
    string resumeText,
    string skill)
{
    Assert.True(SkillMatcher.ContainsSkill(resumeText, skill));
}

[Fact]
public void ContainsSkill_AspNetDoesNotMatchAspNetCore_ReturnsFalse()
{
    var result = SkillMatcher.ContainsSkill(
        "Experienced in ASP.NET Core",
        "ASP.NET");

    Assert.False(result);
}
[Fact]
public void DescriptionRelevanceMatcher_AspNetCoreReceivesTechnicalWeight()
{
    var resumeText = "ASP.NET Core";
    var jobDescription = "ASP.NET Core developer";

    var score = DescriptionRelevanceMatcher.CalculateScore(
        resumeText,
        jobDescription);

    // ASP.NET Core has weight 3, while developer has weight 1.
    // Expected score = (3 / 4) * 100 = 75.
    Assert.Equal(75.0, score);
}


}
