using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeJobMatching.Api.DTOs.Matching;
using ResumeJobMatching.Api.Services.AI;
using System.Security.Claims;
using System.Text.Json;

namespace ResumeJobMatching.Api.Controllers;

[ApiController]
[Route("api/ai-analysis")]
[Authorize]
public class AiAnalysisController : ControllerBase
{
    private readonly GeminiAiService _geminiAiService;

    public AiAnalysisController(
        GeminiAiService geminiAiService)
    {
        _geminiAiService = geminiAiService;
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzeResume(
        [FromBody] ResumeMatchRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        try
        {
            var result = await _geminiAiService.AnalyzeAsync(
                request.ResumeId,
                request.JobId,
                userId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Resume or job not found."
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The AI provider request failed. Check the API configuration and server logs."
                });
        }
        catch (JsonException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The AI provider returned an invalid response."
                });
        }
    }

    [HttpPost("analytics")]
    public async Task<IActionResult> AnalyzeResumeAnalytics(
        [FromBody] ResumeMatchRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        try
        {
            var result = await _geminiAiService.AnalyzeAnalyticsAsync(
                request.ResumeId,
                request.JobId,
                userId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Resume or job not found."
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The AI provider request failed. Check the API configuration and server logs."
                });
        }
        catch (JsonException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message =
                        "The AI provider returned an invalid response."
                });
        }
    }
}
