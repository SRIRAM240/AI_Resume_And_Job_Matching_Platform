using System.ComponentModel.DataAnnotations;

namespace ResumeJobMatching.Api.DTOs.Matching;

public class ResumeMatchRequest
{
    [Range(1, int.MaxValue)]
    public int ResumeId { get; set; }

    [Range(1, int.MaxValue)]
    public int JobId { get; set; }
}