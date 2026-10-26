using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.DTOs.Profile;

namespace ResumeJobMatching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        if (userId is null)
            return Unauthorized();

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        if (userId is null)
            return Unauthorized();

        var user = await _context.Users
            .SingleOrDefaultAsync(
                u => u.Id == userId.Value,
                cancellationToken);

        if (user is null)
            return NotFound(new { message = "User not found." });

        user.FullName = request.FullName.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.CreatedAt,
            message = "Profile updated successfully."
        });
    }

    private int? GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue("sub")
                          ?? User.FindFirstValue(
                              ClaimTypes.NameIdentifier);

        return int.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }
}