using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Infrastructure.Identity;
using RentFlow.Infrastructure.Persistence;
using RentFlow.Domain.Entities;

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
            .ToListAsync(cancellationToken);

        var responses = new List<AdminUserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            responses.Add(new AdminUserResponse(
                user.Id,
                user.Email!,
                user.PhoneNumber,
                user.FirstName,
                user.LastName,
                roles.FirstOrDefault() ?? string.Empty,
                user.AccountStatus.ToString(),
                user.CreatedAtUtc,
                user.UpdatedAtUtc));
        }

        return Ok(responses);
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

    [HttpGet("properties")]
    public async Task<ActionResult<IReadOnlyList<AdminPropertyResponse>>> GetProperties(
        CancellationToken cancellationToken)
    {
        var properties = await _dbContext.Properties.AsNoTracking()
            .Include(property => property.Units)
            .OrderBy(property => property.Name)
            .ToListAsync(cancellationToken);
        var owners = await _userManager.Users
            .Where(user => properties.Select(property => property.OwnerId).Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
        return Ok(properties.Select(property => new AdminPropertyResponse(
            property.Id, property.OwnerId,
            owners.GetValueOrDefault(property.OwnerId) is { } owner
                ? $"{owner.FirstName} {owner.LastName}".Trim() : null,
            property.Name, property.Address, property.City, property.Status,
            property.Units.Count, property.Units.Count(unit => unit.Status == UnitStatus.Occupied))));
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IReadOnlyList<AdminApplicationResponse>>> GetApplications(
        CancellationToken cancellationToken)
    {
        var applications = await _dbContext.RentalApplications.AsNoTracking()
            .Include(application => application.Unit).ThenInclude(unit => unit.Property)
            .OrderByDescending(application => application.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
        var users = await _userManager.Users
            .Where(user => applications.Select(application => application.TenantId).Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
        return Ok(applications.Select(application =>
        {
            var tenant = users.GetValueOrDefault(application.TenantId);
            return new AdminApplicationResponse(
                application.Id, application.Status, application.TenantId,
                tenant is null ? null : $"{tenant.FirstName} {tenant.LastName}".Trim(),
                application.Unit.PropertyId, application.Unit.Property.Name,
                application.Unit.NameOrNumber, application.SubmittedAtUtc,
                application.ReviewedAtUtc);
        }));
    }

    [HttpPost("properties/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendProperty(Guid id, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (property is null)
            return NotFound();
        property.Status = PropertyStatus.Suspended;
        property.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { propertyId = id, status = property.Status });
    }

    [HttpDelete("properties/{id:guid}")]
    public async Task<IActionResult> RemoveProperty(Guid id, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties
            .Include(item => item.Units).ThenInclude(unit => unit.Applications)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (property is null)
            return NotFound();
        if (property.Units.Any(unit => unit.Status == UnitStatus.Occupied))
            return Conflict("A property with occupied units cannot be removed.");
        _dbContext.RentalApplications.RemoveRange(property.Units.SelectMany(unit => unit.Applications));
        _dbContext.Properties.Remove(property);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

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
    string Role,
    string AccountStatus,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record AdminPropertyResponse(
    Guid Id,
    string OwnerId,
    string? OwnerName,
    string Name,
    string Address,
    string City,
    PropertyStatus Status,
    int UnitCount,
    int OccupiedUnitCount);

public sealed record AdminApplicationResponse(
    Guid Id,
    RentalApplicationStatus Status,
    string TenantId,
    string? TenantName,
    Guid PropertyId,
    string PropertyName,
    string UnitNameOrNumber,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc);
