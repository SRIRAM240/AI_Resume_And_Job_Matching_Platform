namespace ResumeJobMatching.Api.DTOs.AI;

public class AiAnalyticsCategoryScores
{
    public int TechnicalSkills { get; set; }
    public int ExperienceRelevance { get; set; }
    public int EducationAndCertifications { get; set; }
    public int SoftSkillsAndLeadership { get; set; }
    public int OverallFit { get; set; }
}

public class AiAnalyticsResponse
{
    public int ResumeId { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public AiAnalyticsCategoryScores CategoryScores { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public List<string> InterviewFocusAreas { get; set; } = new();
}

