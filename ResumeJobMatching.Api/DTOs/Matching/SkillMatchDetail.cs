
namespace ResumeJobMatching.Api.DTOs.Matching;

public class SkillMatchDetail
{
    public string Skill { get; set; } = string.Empty;

    public bool IsMatched { get; set; }

    public string Explanation { get; set; } = string.Empty;
}