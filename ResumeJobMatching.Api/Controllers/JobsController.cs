using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeJobMatching.Api.DTOs.Jobs;
using ResumeJobMatching.Api.Services.Jobs;

namespace ResumeJobMatching.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly JobService _jobService;

    public JobsController(JobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _jobService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = job.Id },
            job);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var jobs = await _jobService.GetAllAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Job ID must be greater than zero."
            });
        }

        var job = await _jobService.GetByIdAsync(id, cancellationToken);

        if (job is null)
        {
            return NotFound(new { message = "Job not found." });
        }

        return Ok(job);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Job ID must be greater than zero."
            });
        }

        var job = await _jobService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (job is null)
        {
            return NotFound(new { message = "Job not found." });
        }

        return Ok(job);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Job ID must be greater than zero."
            });
        }

        var deleted = await _jobService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { message = "Job not found." });
        }

        return NoContent();
    }
}