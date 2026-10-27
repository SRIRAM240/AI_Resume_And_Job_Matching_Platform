using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeJobMatching.Api.DTOs.Matching;
using ResumeJobMatching.Api.Services.Matching;

namespace ResumeJobMatching.Api.Controllers;

[ApiController]
[Route("api/matching")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly ResumeMatchingService _matchingService;

    public MatchingController(ResumeMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpPost]
    public async Task<IActionResult> MatchResume(
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
            var result = await _matchingService.MatchAsync(
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
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("resume/{resumeId:int}/jobs")]
    public async Task<IActionResult> MatchAllJobs(
    int resumeId,
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
            var results = await _matchingService.MatchAllJobsAsync(
                resumeId,
                userId,
                cancellationToken);

            return Ok(results);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}