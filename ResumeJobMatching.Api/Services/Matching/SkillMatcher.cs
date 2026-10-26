using System.Text.RegularExpressions;

namespace ResumeJobMatching.Api.Services.Matching;

public static class SkillMatcher
{
    public static bool ContainsSkill(
        string resumeText,
        string skill)
    {
        if (string.IsNullOrWhiteSpace(resumeText) ||
            string.IsNullOrWhiteSpace(skill))
        {
            return false;
        }

        var normalizedResume = Normalize(resumeText);
        var normalizedSkill = Normalize(skill);

        var escapedSkill = Regex.Escape(normalizedSkill);

        var pattern =
            $@"(?<![\p{{L}}\p{{N}}]){escapedSkill}(?![\p{{L}}\p{{N}}])";

        return Regex.IsMatch(
            normalizedResume,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.ToLowerInvariant();

        // Normalize C# variations.
        normalized = Regex.Replace(
            normalized,
            @"(?<![a-z0-9])c\s*(?:#|sharp)(?![a-z0-9])",
            "csharp");

        // Normalize ASP.NET variations.
        normalized = Regex.Replace(
            normalized,
            @"\basp\s*\.?\s*net\b",
            "aspnet");

        // Normalize Entity Framework variations.
        normalized = Regex.Replace(
            normalized,
            @"\bentity\s*framework\b",
            "entityframework");

        // Normalize SQL Server variations.
        normalized = Regex.Replace(
            normalized,
            @"\b(?:microsoft\s+)?sql\s*server\b",
            "sqlserver");

        // Normalize REST API variations.
        normalized = Regex.Replace(
            normalized,
            @"\brest(?:ful)?\s+apis?\b",
            "rest api");

        // Normalize whitespace.
        normalized = Regex.Replace(
            normalized,
            @"\s+",
            " ").Trim();

        return normalized;
    }
}
