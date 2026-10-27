
using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.Matching;
using ResumeJobMatching.Api.Models.Jobs;

namespace ResumeJobMatching.Api.Services.Matching;

public class ResumeMatchingService
{
    private readonly ApplicationDbContext _context;

    private const double SkillScoreWeight = 0.70;
private const double DescriptionScoreWeight = 0.30;
private const double DefaultSkillWeight = 10;



    private static readonly Dictionary<string, double> SkillWeights =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["C#"] = 30,
            ["ASP.NET Core"] = 30,
            ["Web API"] = 20,
            ["Entity Framework Core"] = 20,
            ["SQL Server"] = 20,
            ["Git"] = 10,
            ["Docker"] = 10,
            ["AWS"] = 15,
            ["Azure"] = 15,
            ["Java"] = 30,
            ["Spring Boot"] = 25,
            ["PostgreSQL"] = 20
        };


    public ResumeMatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static double GetSkillWeight(string skill)
    {
        return SkillWeights.TryGetValue(skill.Trim(), out var weight)
            ? weight
            : DefaultSkillWeight;
    }

    public async Task<ResumeMatchResponse?> MatchAsync(
        int resumeId,
        int jobId,
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

        ValidateExtractedText(resume.ExtractedText);

        var job = await _context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                j => j.Id == jobId,
                cancellationToken);

        if (job is null)
        {
            return null;
        }

        return BuildMatchResponse(resume, job);
    }

    public async Task<List<ResumeMatchResponse>> MatchAllJobsAsync(
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
            throw new KeyNotFoundException("Resume not found.");
        }

        ValidateExtractedText(resume.ExtractedText);

        var jobs = await _context.Jobs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return jobs
            .Select(job => BuildMatchResponse(resume, job))
            .OrderByDescending(result => result.MatchScore)
            .ThenBy(result => result.JobTitle)
            .ToList();
    }

    private static void ValidateExtractedText(string? extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException(
                "The selected resume does not contain extracted text.");
        }
    }

    private static ResumeMatchResponse BuildMatchResponse(
        global::ResumeJobMatching.Api.Models.Resume resume,
        Job job)
    {
        var requiredSkills = (job.RequiredSkills ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchingSkills = new List<string>();
        var missingSkills = new List<string>();
        var skillMatchDetails = new List<SkillMatchDetail>();

        foreach (var skill in requiredSkills)
        {
            var isMatched = SkillMatcher.ContainsSkill(
                resume.ExtractedText!,
                skill);

            skillMatchDetails.Add(new SkillMatchDetail
            {
                Skill = skill,
                IsMatched = isMatched,
                Explanation = isMatched
                    ? $"The required skill '{skill}' was found in the extracted resume text."
                    : $"No matching evidence for '{skill}' was found in the extracted resume text."
            });

            if (isMatched)
            {
                matchingSkills.Add(skill);
            }
            else
            {
                missingSkills.Add(skill);
            }
        }

        var totalWeight = requiredSkills.Sum(GetSkillWeight);
        var matchedWeight = matchingSkills.Sum(GetSkillWeight);

        var skillMatchScore = totalWeight == 0
            ? 0.0
            : Math.Round(
                matchedWeight * 100.0 / totalWeight,
                2);

        var descriptionRelevanceScore =
            DescriptionRelevanceMatcher.CalculateScore(
                resume.ExtractedText!,
                job.Description ?? string.Empty);

        var matchScore = requiredSkills.Count == 0
    ? 0.0
    : Math.Round(
        Math.Clamp(
            (skillMatchScore * SkillScoreWeight) +
            (descriptionRelevanceScore * DescriptionScoreWeight),
            0,
            100),
        2);

        var summary = requiredSkills.Count == 0
    ? "No required skills were specified for this job."
    : $"Matched {matchingSkills.Count} of {requiredSkills.Count} required skills. " +
      $"Missing {missingSkills.Count} required skills.";

        return new ResumeMatchResponse
        {
            ResumeId = resume.Id,
            ResumeFileName = resume.FileName,
            JobId = job.Id,
            JobTitle = job.Title,
            CompanyName = job.CompanyName,

            SkillMatchScore = skillMatchScore,
            DescriptionRelevanceScore = descriptionRelevanceScore,
            MatchScore = matchScore,

            MatchingSkills = matchingSkills,
            MissingSkills = missingSkills,
            SkillMatchDetails = skillMatchDetails,
            Summary = summary
        };
    }
}
