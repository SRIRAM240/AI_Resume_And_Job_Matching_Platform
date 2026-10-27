using System.Text.RegularExpressions;

namespace ResumeJobMatching.Api.Services.Matching;

public static class DescriptionRelevanceMatcher
{
    private const double GenericTermWeight = 1.0;
    private const double TechnicalTermWeight = 3.0;

    private static readonly HashSet<string> StopWords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "been",
            "being", "by", "for", "from", "has", "have", "had",
            "in", "is", "it", "its", "of", "on", "or", "our",
            "the", "to", "using", "use", "we", "with", "will",
            "you", "your", "this", "that", "these", "those",
            "their", "they", "them", "who", "what", "when",
            "where", "which", "while", "how", "can", "could",
            "should", "would", "must", "may", "might",
            "work", "working", "role", "responsibilities",
            "responsibility", "position", "candidate",
            "candidates", "company", "team", "teams",
            "looking", "seeking", "required", "requirements",
            "preferred", "ability", "abilities", "strong",
            "good", "excellent", "including", "etc"
        };

    private static readonly HashSet<string> TechnicalTerms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "csharp", "aspnet", "aspnetcore", "javascript",
            "typescript", "java", "python", "dotnet",
            "api", "rest", "docker", "kubernetes", "aws",
            "azure", "gcp", "postgresql", "mysql", "mongodb",
            "redis", "git", "linux", "html", "css", "react",
            "angular", "node", "express", "microservices",
            "devops", "transformers", "langchain",
            "sqlserver", "entityframework", "efcore",
            "webapi", "sql", "database", "databases",
            "machine", "learning", "ai", "ml",
            "tensorflow", "pytorch", "huggingface",
            "rag", "llm", "llms", "restful"
        };

    public static double CalculateScore(
        string resumeText,
        string jobDescription)
    {
        if (string.IsNullOrWhiteSpace(resumeText) ||
            string.IsNullOrWhiteSpace(jobDescription))
        {
            return 0;
        }

        var descriptionTerms = ExtractTerms(jobDescription);

        if (descriptionTerms.Count == 0)
        {
            return 0;
        }

        var resumeTerms = ExtractTerms(resumeText);

        var totalWeight = descriptionTerms.Sum(GetTermWeight);

        var matchedWeight = descriptionTerms
            .Where(resumeTerms.Contains)
            .Sum(GetTermWeight);

        if (totalWeight == 0)
        {
            return 0;
        }

        return Math.Round(
            Math.Clamp(matchedWeight * 100.0 / totalWeight, 0, 100),
            2);
    }

    private static double GetTermWeight(string term)
    {
        return TechnicalTerms.Contains(term)
            ? TechnicalTermWeight
            : GenericTermWeight;
    }

    private static string NormalizeTechnicalTerms(string text)
    {
        var normalized = text.ToLowerInvariant();

        // Normalize C# and C Sharp.
        normalized = Regex.Replace(
            normalized,
            @"(?<![a-z0-9])c\s*(?:#(?![a-z0-9])|sharp\b)",
            "csharp",
            RegexOptions.IgnoreCase);

        // Normalize ASP.NET Core before ASP.NET.
        normalized = Regex.Replace(
            normalized,
            @"\basp\s*\.?\s*net\s+core\b",
            "aspnetcore",
            RegexOptions.IgnoreCase);

        normalized = Regex.Replace(
            normalized,
            @"\basp\s*\.?\s*net\b",
            "aspnet",
            RegexOptions.IgnoreCase);

        // Normalize SQL Server.
        normalized = Regex.Replace(
            normalized,
            @"\b(?:microsoft\s+)?sql\s+server\b",
            "sqlserver",
            RegexOptions.IgnoreCase);

        // Normalize Entity Framework Core.
        normalized = Regex.Replace(
            normalized,
            @"\bentity\s+framework\s+core\b|\bef\s+core\b",
            "efcore",
            RegexOptions.IgnoreCase);

        normalized = Regex.Replace(
            normalized,
            @"\bentity\s+framework\b",
            "entityframework",
            RegexOptions.IgnoreCase);

        // Normalize JavaScript aliases.
        normalized = Regex.Replace(
            normalized,
            @"\bjs\b",
            "javascript",
            RegexOptions.IgnoreCase);

        // Normalize PostgreSQL aliases.
        normalized = Regex.Replace(
            normalized,
            @"\bpostgres\b",
            "postgresql",
            RegexOptions.IgnoreCase);

        // Normalize RESTful terminology.
        normalized = Regex.Replace(
            normalized,
            @"\brestful\b",
            "rest",
            RegexOptions.IgnoreCase);

        // Normalize Web API terminology.
        normalized = Regex.Replace(
            normalized,
            @"\bweb\s+apis?\b",
            "webapi",
            RegexOptions.IgnoreCase);

        // Normalize .NET.
        normalized = Regex.Replace(
            normalized,
            @"(?<![a-z0-9])\.net(?![a-z0-9])",
            "dotnet",
            RegexOptions.IgnoreCase);

        // Normalize whitespace.
        normalized = Regex.Replace(
            normalized,
            @"\s+",
            " ").Trim();

        return normalized;
    }

    private static string NormalizeTerm(string term)
    {
        return term.ToLowerInvariant() switch
        {
            "apis" => "api",
            "llms" => "llm",
            "databases" => "database",
            "microservice" => "microservices",
            "experienced" => "experience",
            _ => term.ToLowerInvariant()
        };
    }

    private static HashSet<string> ExtractTerms(string text)
    {
        var normalizedText = NormalizeTechnicalTerms(text);

        return Regex
            .Matches(
                normalizedText,
                @"[a-z0-9]+(?:[+#.][a-z0-9+#.]*)*|[+#]+")
            .Select(match => match.Value.Trim('.'))
            .Select(NormalizeTerm)
            .Where(term =>
                term.Length > 1 &&
                !StopWords.Contains(term))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
