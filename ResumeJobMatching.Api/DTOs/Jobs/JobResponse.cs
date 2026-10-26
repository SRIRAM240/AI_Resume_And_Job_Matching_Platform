namespace ResumeJobMatching.Api.DTOs.Jobs;

public class JobResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RequiredSkills { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}