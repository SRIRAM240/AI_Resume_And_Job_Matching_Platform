namespace ResumeJobMatching.Api.DTOs.Matching;

public class ResumeMatchResponse
{
    public int ResumeId { get; set; }

    public string ResumeFileName { get; set; } = string.Empty;

    public int JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public double MatchScore { get; set; }

    public List<string> MatchingSkills { get; set; } = [];

    public List<string> MissingSkills { get; set; } = [];
    public List<SkillMatchDetail> SkillMatchDetails { get; set; } = [];

    public string Summary { get; set; } = string.Empty;
}