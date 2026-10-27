
namespace ResumeJobMatching.Api.DTOs.AI;

public class AiAnalysisResponse
{
    public int ResumeId { get; set; }

    public int JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Analysis { get; set; } = string.Empty;
}
