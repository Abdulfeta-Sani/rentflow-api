using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Infrastructure.Identity;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RentFlowDbContext _dbContext;

    public AdminController(
        UserManager<AppUser> userManager,
        RentFlowDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new AdminUserResponse(
                user.Id,
                user.Email!,
                user.PhoneNumber,
                user.FirstName,
                user.LastName,
                user.AccountStatus,
                user.CreatedAtUtc,
                user.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("users/{id}/activate")]
    public Task<IActionResult> ActivateUser(
        string id,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, AccountStatus.Active, false, cancellationToken);

    [HttpPost("users/{id}/suspend")]
    public Task<IActionResult> SuspendUser(
        string id,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, AccountStatus.Suspended, true, cancellationToken);

    [HttpPost("users/{id}/deactivate")]
    public Task<IActionResult> DeactivateUser(
        string id,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, AccountStatus.Deactivated, true, cancellationToken);

    private async Task<IActionResult> ChangeStatusAsync(
        string id,
        AccountStatus status,
        bool revokeRefreshTokens,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        user.AccountStatus = status;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        if (revokeRefreshTokens)
        {
            var refreshTokens = await _dbContext.RefreshTokens
                .Where(token =>
                    token.UserId == user.Id &&
                    token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var refreshToken in refreshTokens)
                refreshToken.RevokedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            message = $"User {status.ToString().ToLowerInvariant()}.",
            userId = user.Id,
            accountStatus = user.AccountStatus.ToString()
        });
    }
}

public sealed record AdminUserResponse(
    string Id,
    string Email,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    AccountStatus AccountStatus,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
