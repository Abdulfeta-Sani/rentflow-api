using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/tenancies")]
public sealed class TenanciesController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;

    public TenanciesController(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOnly)]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<TenancyResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var tenantId = GetCurrentUserId();

        if (tenantId is null)
            return Unauthorized();

        var tenancies = await _dbContext.Tenancies
            .AsNoTracking()
            .Include(tenancy => tenancy.Unit)
            .ThenInclude(unit => unit.Property)
            .Where(tenancy => tenancy.TenantId == tenantId)
            .OrderByDescending(tenancy => tenancy.StartDate)
            .Select(tenancy => ToResponse(tenancy))
            .ToListAsync(cancellationToken);

        return Ok(tenancies);
    }

    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    [HttpGet("owner")]
    public async Task<ActionResult<IReadOnlyList<TenancyResponse>>> GetOwnerTenancies(
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();

        if (ownerId is null)
            return Unauthorized();

        var tenancies = await _dbContext.Tenancies
            .AsNoTracking()
            .Include(tenancy => tenancy.Unit)
            .ThenInclude(unit => unit.Property)
            .Where(tenancy => tenancy.OwnerId == ownerId)
            .OrderByDescending(tenancy => tenancy.StartDate)
            .Select(tenancy => ToResponse(tenancy))
            .ToListAsync(cancellationToken);

        return Ok(tenancies);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenancyResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var tenancy = await _dbContext.Tenancies
            .AsNoTracking()
            .Include(item => item.Unit)
            .ThenInclude(unit => unit.Property)
            .SingleOrDefaultAsync(
                item => item.Id == id &&
                        (item.TenantId == userId || item.OwnerId == userId),
                cancellationToken);

        return tenancy is null
            ? NotFound()
            : Ok(ToResponse(tenancy));
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static TenancyResponse ToResponse(Tenancy tenancy) =>
        new(
            tenancy.Id,
            tenancy.ApplicationId,
            tenancy.UnitId,
            tenancy.Unit.NameOrNumber,
            tenancy.Unit.PropertyId,
            tenancy.Unit.Property.Name,
            tenancy.Unit.Property.Address,
            tenancy.Unit.Property.City,
            tenancy.TenantId,
            tenancy.OwnerId,
            tenancy.StartDate,
            tenancy.EndDate,
            tenancy.MonthlyRent,
            tenancy.SecurityDeposit,
            tenancy.Status,
            tenancy.CreatedAtUtc,
            tenancy.ActivatedAtUtc,
            tenancy.EndedAtUtc);
}

public sealed record TenancyResponse(
    Guid Id,
    Guid ApplicationId,
    Guid UnitId,
    string UnitNameOrNumber,
    Guid PropertyId,
    string PropertyName,
    string PropertyAddress,
    string PropertyCity,
    string TenantId,
    string OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MonthlyRent,
    decimal SecurityDeposit,
    TenancyStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ActivatedAtUtc,
    DateTime? EndedAtUtc);