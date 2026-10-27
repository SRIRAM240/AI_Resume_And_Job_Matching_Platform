
using System.Text.RegularExpressions;

namespace ResumeJobMatching.Api.Services.Matching;

public static class SkillMatcher
{
    private static readonly Dictionary<string, string[]> SkillAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["C#"] = ["C#", "C Sharp", "CSharp"],

            ["ASP.NET Core"] =
                ["ASP.NET Core", "ASP Net Core", "ASPNetCore", "aspnetcore"],

            ["Web API"] =
                ["Web API", "Web APIs", "ASP.NET Core Web API"],

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

        var aliases = SkillAliases.TryGetValue(
            skill.Trim(),
            out var knownAliases)
                ? knownAliases
                : [skill];

        foreach (var alias in aliases)
        {
            var normalizedSkill = Normalize(alias);

            if (string.IsNullOrWhiteSpace(normalizedSkill))
            {
                continue;
            }

            var pattern =
                $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(normalizedSkill)}(?![\p{{L}}\p{{N}}])";

            if (Regex.IsMatch(
                normalizedResume,
                pattern,
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
            {
                return true;
            }
        }

        return false;
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

        // Normalize ASP.NET Core before ASP.NET.
        normalized = Regex.Replace(
            normalized,
            @"\basp\s*\.?\s*net\s+core\b",
            "aspnetcore");

        normalized = Regex.Replace(
            normalized,
            @"\basp\s*\.?\s*net\b",
            "aspnet");

        // Normalize Entity Framework Core and EF Core.
        normalized = Regex.Replace(
            normalized,
            @"\bentity\s+framework\s+core\b",
            "efcore");

        normalized = Regex.Replace(
            normalized,
            @"\bef\s*core\b",
            "efcore");

        normalized = Regex.Replace(
            normalized,
            @"\bentity\s*framework\b",
            "entityframework");

        // Normalize SQL Server, but keep plain SQL separate.
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
