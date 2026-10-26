using System.ComponentModel.DataAnnotations;

namespace ResumeJobMatching.Api.DTOs.Jobs;

public class UpdateJobRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [MinLength(20)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 2)]
    public string RequiredSkills { get; set; } = string.Empty;

    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(50)]
    public string EmploymentType { get; set; } = string.Empty;
}