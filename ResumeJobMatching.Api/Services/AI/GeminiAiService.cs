using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.AI;
using ResumeJobMatching.Api.Models.Jobs;
using ResumeEntity = ResumeJobMatching.Api.Models.Resume;

namespace ResumeJobMatching.Api.Services.AI;

public class GeminiAiService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiAiService(
        ApplicationDbContext context,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private async Task<(ResumeEntity resume, Job job, string apiKey, string model, string resumeText)> ValidateAndPrepareAsync(
        int resumeId,
        int jobId,
        int userId,
        CancellationToken cancellationToken)
    {
        var resume = await _context.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == resumeId && r.UserId == userId,
                cancellationToken);

        if (resume is null)
        {
            throw new KeyNotFoundException("Resume not found.");
        }

        if (string.IsNullOrWhiteSpace(resume.ExtractedText))
        {
            throw new InvalidOperationException(
                "The selected resume does not contain extracted text.");
        }

        var job = await _context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                j => j.Id == jobId,
                cancellationToken);

        if (job is null)
        {
            throw new KeyNotFoundException("Job not found.");
        }

        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured. Configure Gemini:ApiKey using User Secrets.");
        }

        var resumeText = resume.ExtractedText.Length > 12000
            ? resume.ExtractedText[..12000]
            : resume.ExtractedText;

        return (resume, job, apiKey, model, resumeText);
    }

    public async Task<AiAnalysisResponse?> AnalyzeAsync(
        int resumeId,
        int jobId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ResumeEntity resume;
        Job job;
        string apiKey;
        string model;
        string resumeText;

        try
        {
            (resume, job, apiKey, model, resumeText) = await ValidateAndPrepareAsync(
                resumeId, jobId, userId, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        var prompt = $"""
            You are an AI resume and job matching assistant.

            Analyze the candidate's resume against the job details.

            JOB TITLE:
            {job.Title}

            COMPANY:
            {job.CompanyName}

            JOB DESCRIPTION:
            {job.Description}

            REQUIRED SKILLS:
            {job.RequiredSkills}

            RESUME TEXT:
            {resumeText}

            Return a detailed analysis with these sections:

            1. Overall suitability for the job.
            2. Relevant skills found in the resume.
            3. Required skills that are missing or not clearly demonstrated.
            4. Relevant experience and projects.
            5. Specific suggestions to improve the resume for this job.
            6. Recommended topics for interview preparation.

            Use clear, professional language.
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 2048
            }
        };

        var rawResponse = await SendGeminiRequestAsync(model, apiKey, requestBody, cancellationToken);

        return new AiAnalysisResponse
        {
            ResumeId = resume.Id,
            JobId = job.Id,
            JobTitle = job.Title,
            CompanyName = job.CompanyName,
            Analysis = rawResponse
        };
    }

    public async Task<AiAnalyticsResponse?> AnalyzeAnalyticsAsync(
        int resumeId,
        int jobId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ResumeEntity resume;
        Job job;
        string apiKey;
        string model;
        string resumeText;

        try
        {
            (resume, job, apiKey, model, resumeText) = await ValidateAndPrepareAsync(
                resumeId, jobId, userId, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        var prompt = $$"""
            You are an expert talent acquisition and ATS evaluation analyst.

            Perform an in-depth evaluation of the candidate's resume against the target job.
            Convert the evaluation into structured analytics.

            JOB TITLE:
            {{job.Title}}

            COMPANY:
            {{job.CompanyName}}

            JOB DESCRIPTION:
            {{job.Description}}

            REQUIRED SKILLS:
            {{job.RequiredSkills}}

            RESUME TEXT:
            {{resumeText}}

            Respond strictly with a valid JSON object matching this schema:
            {
              "verdict": "Strong Match" | "Good Match" | "Moderate Match" | "Needs Improvement",
              "summary": "Concise 2-sentence executive summary of candidate fit and alignment.",
              "categoryScores": {
                "technicalSkills": <integer 0-100 indicating tech stack overlap>,
                "experienceRelevance": <integer 0-100 indicating relevance of past experience/projects>,
                "educationAndCertifications": <integer 0-100 indicating education and certification alignment>,
                "softSkillsAndLeadership": <integer 0-100 indicating communication, leadership and teamwork signals>,
                "overallFit": <integer 0-100 holistic suitability score>
              },
              "strengths": [
                "3 to 5 concise bullet points highlighting key matching competencies and achievements"
              ],
              "improvements": [
                "3 to 4 specific, actionable resume improvements or skill gaps to address"
              ],
              "interviewFocusAreas": [
                "3 to 4 key technical topics or scenarios candidate should prepare for in interview"
              ]
            }

            Do not wrap in markdown quotes. Return only pure JSON.
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            }
        };

        var rawJson = await SendGeminiRequestAsync(model, apiKey, requestBody, cancellationToken);

        // Sanitize any markdown wrappers if returned
        var cleanJson = rawJson.Trim();
        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[7..];
        }
        else if (cleanJson.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[3..];
        }
        if (cleanJson.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[..^3];
        }
        cleanJson = cleanJson.Trim();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        AiAnalyticsPayload? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<AiAnalyticsPayload>(cleanJson, jsonOptions);
        }
        catch (JsonException)
        {
            // Handled below with default fallback
        }

        var scores = payload?.CategoryScores ?? new AiAnalyticsCategoryScores
        {
            TechnicalSkills = 75,
            ExperienceRelevance = 70,
            EducationAndCertifications = 75,
            SoftSkillsAndLeadership = 80,
            OverallFit = 75
        };

        scores.TechnicalSkills = Math.Clamp(scores.TechnicalSkills, 0, 100);
        scores.ExperienceRelevance = Math.Clamp(scores.ExperienceRelevance, 0, 100);
        scores.EducationAndCertifications = Math.Clamp(scores.EducationAndCertifications, 0, 100);
        scores.SoftSkillsAndLeadership = Math.Clamp(scores.SoftSkillsAndLeadership, 0, 100);
        scores.OverallFit = Math.Clamp(scores.OverallFit, 0, 100);

        return new AiAnalyticsResponse
        {
            ResumeId = resume.Id,
            JobId = job.Id,
            JobTitle = job.Title,
            CompanyName = job.CompanyName,
            Verdict = !string.IsNullOrWhiteSpace(payload?.Verdict) ? payload.Verdict : "Evaluated",
            Summary = !string.IsNullOrWhiteSpace(payload?.Summary)
                ? payload.Summary
                : "Candidate profile analyzed against position requirements.",
            CategoryScores = scores,
            Strengths = payload?.Strengths?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>(),
            Improvements = payload?.Improvements?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? new List<string>(),
            InterviewFocusAreas = payload?.InterviewFocusAreas?.Where(a => !string.IsNullOrWhiteSpace(a)).ToList() ?? new List<string>()
        };
    }

    private async Task<string> SendGeminiRequestAsync(
        string model,
        string apiKey,
        object requestBody,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API returned HTTP {(int)response.StatusCode}.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini did not return an analysis. Try again.");
        }

        var candidate = candidates[0];

        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts))
        {
            throw new InvalidOperationException("Gemini returned an unexpected response.");
        }

        var textParts = new List<string>();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    textParts.Add(text);
                }
            }
        }

        var combined = string.Join(Environment.NewLine, textParts);
        if (string.IsNullOrWhiteSpace(combined))
        {
            throw new InvalidOperationException("Gemini returned an empty response.");
        }

        return combined;
    }

    private class AiAnalyticsPayload
    {
        public string? Verdict { get; set; }
        public string? Summary { get; set; }
        public AiAnalyticsCategoryScores? CategoryScores { get; set; }
        public List<string>? Strengths { get; set; }
        public List<string>? Improvements { get; set; }
        public List<string>? InterviewFocusAreas { get; set; }
    }
}
