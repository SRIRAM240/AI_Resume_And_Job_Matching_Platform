using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeJobMatching.Api.DTOs.Resume;
using ResumeJobMatching.Api.Services.Resume;

namespace ResumeJobMatching.Api.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumesController : ControllerBase
{
    private readonly ResumeService _resumeService;

    public ResumesController(ResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] ResumeUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new
            {
                message = "Please select a resume file."
            });
        }

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
            var response = await _resumeService.UploadAsync(
                request.File,
                userId,
                cancellationToken);

            return Created(
                $"/api/resumes/{response.Id}",
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidDataException)
        {
            return BadRequest(new
            {
                message = "The uploaded document could not be processed."
            });
        }
        catch (Exception ex) when (
            ex is UglyToad.PdfPig.Core.PdfDocumentFormatException ||
            ex is DocumentFormat.OpenXml.Packaging.OpenXmlPackageException)
        {
            return BadRequest(new
            {
                message = "The uploaded document is invalid or corrupted."
            });
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetMyResumes(
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

        // Retrieve only resumes belonging to the authenticated user.
        var resumes = await _resumeService.GetMyResumesAsync(
            userId,
            cancellationToken);

        return Ok(resumes);
    }
    // GET: api/resumes/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResumeById(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Resume ID must be greater than zero."
            });
        }

        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        var resume = await _resumeService.GetResumeByIdAsync(
            id,
            userId,
            cancellationToken);

        if (resume is null)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }

        return Ok(resume);
    }
    // GET: api/resumes/{id}/download
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> DownloadResume(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Resume ID must be greater than zero."
            });
        }

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
            var result = await _resumeService.DownloadAsync(
                id,
                userId,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Resume not found."
                });
            }

            var contentType = Path.GetExtension(result.Value.FileName)
                .ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            return File(
                result.Value.Content,
                contentType,
                result.Value.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new
            {
                message = "The stored resume file could not be found."
            });
        }
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteResume(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Resume ID must be greater than zero."
            });
        }

        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        var deleted = await _resumeService.DeleteAsync(
            id,
            userId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }

        return NoContent();
    }
}