
using System.ComponentModel.DataAnnotations;

namespace ResumeJobMatching.Api.DTOs.Profile;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;
}
