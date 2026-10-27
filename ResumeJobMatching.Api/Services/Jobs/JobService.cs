using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.Jobs;
using ResumeJobMatching.Api.Models.Jobs;

namespace ResumeJobMatching.Api.Services.Jobs;

public class JobService
{
    private readonly ApplicationDbContext _context;

    public JobService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobResponse> CreateAsync(

        CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredFields(
            request.Title,
            request.CompanyName,
            request.Description,
            request.RequiredSkills);
        var job = new Job
        {
            Title = request.Title.Trim(),
            CompanyName = request.CompanyName.Trim(),
            Description = request.Description.Trim(),
            RequiredSkills = request.RequiredSkills.Trim(),
            Location = request.Location.Trim(),
            EmploymentType = request.EmploymentType.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(job);
    }

    public async Task<List<JobResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Jobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAt)
            .Select(job => new JobResponse
            {
                Id = job.Id,
                Title = job.Title,
                CompanyName = job.CompanyName,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                Location = job.Location,
                EmploymentType = job.EmploymentType,
                CreatedAt = job.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<JobResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

        return job is null ? null : MapToResponse(job);
    }

    public async Task<JobResponse?> UpdateAsync(
        int id,
        UpdateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredFields(
    request.Title,
    request.CompanyName,
    request.Description,
    request.RequiredSkills);
        var job = await _context.Jobs
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Title = request.Title.Trim();
        job.CompanyName = request.CompanyName.Trim();
        job.Description = request.Description.Trim();
        job.RequiredSkills = request.RequiredSkills.Trim();
        job.Location = request.Location.Trim();
        job.EmploymentType = request.EmploymentType.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(job);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

        if (job is null)
        {
            return false;
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static JobResponse MapToResponse(Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            Title = job.Title,
            CompanyName = job.CompanyName,
            Description = job.Description,
            RequiredSkills = job.RequiredSkills,
            Location = job.Location,
            EmploymentType = job.EmploymentType,
            CreatedAt = job.CreatedAt
        };
    }
    private static void ValidateRequiredFields(
    string? title,
    string? companyName,
    string? description,
    string? requiredSkills)
{
    if (string.IsNullOrWhiteSpace(title))
        throw new ArgumentException("Job title is required.", nameof(title));

    if (string.IsNullOrWhiteSpace(companyName))
        throw new ArgumentException("Company name is required.", nameof(companyName));

    if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentException("Job description is required.", nameof(description));

    if (string.IsNullOrWhiteSpace(requiredSkills))
        throw new ArgumentException("Required skills must be specified.", nameof(requiredSkills));
}
}