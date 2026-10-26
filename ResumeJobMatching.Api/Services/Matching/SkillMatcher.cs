
using System.Text.RegularExpressions;

namespace ResumeJobMatching.Api.Services.Matching;

public static class SkillMatcher
{
    private static readonly Dictionary<string, string[]> SkillAliases =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["C#"] = ["C#", "C Sharp", "CSharp"],

        ["ASP.NET Core"] =
            ["ASP.NET Core", "ASP Net Core"],

        ["Web API"] =
            ["Web API", "Web APIs"],

        ["Entity Framework Core"] =
            ["Entity Framework Core", "EF Core"],

        ["SQL Server"] =
            ["SQL Server", "Microsoft SQL Server"],

        ["REST APIs"] =
            ["REST API", "REST APIs", "RESTful API", "RESTful APIs"],

        ["AWS"] =
            ["AWS", "Amazon Web Services"],

        ["PostgreSQL"] =
            ["PostgreSQL", "Postgres"]
    };

    public static bool ContainsSkill(string resumeText, string skill)
    {
        if (string.IsNullOrWhiteSpace(resumeText) ||
            string.IsNullOrWhiteSpace(skill))
        {
            return false;
        }

        var normalizedResume = Normalize(resumeText);

        var aliases = SkillAliases.TryGetValue(skill.Trim(), out var knownAliases)
            ? knownAliases
            : [skill];

        return aliases.Any(alias =>
        {
            var normalizedSkill = Normalize(alias);

            var pattern =
                $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(normalizedSkill)}(?![\p{{L}}\p{{N}}])";

            return Regex.IsMatch(
                normalizedResume,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        });
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
