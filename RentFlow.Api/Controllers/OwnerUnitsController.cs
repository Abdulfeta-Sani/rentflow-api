using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Application.DTOs.Units;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/owner")]
[Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
public sealed class OwnerUnitsController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;

    public OwnerUnitsController(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("properties/{propertyId:guid}/units")]
    public async Task<ActionResult<IReadOnlyList<UnitResponse>>> GetUnits(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var ownsProperty = await _dbContext.Properties.AnyAsync(
            property => property.Id == propertyId &&
                        property.OwnerId == ownerId,
            cancellationToken);

        if (!ownsProperty)
            return NotFound();

        var units = await _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId)
            .OrderBy(unit => unit.NameOrNumber)
            .Select(unit => new UnitResponse(
                unit.Id,
                unit.PropertyId,
                unit.NameOrNumber,
                unit.Bedrooms,
                unit.Bathrooms,
                unit.MonthlyRent,
                unit.SecurityDeposit,
                unit.Status))
            .ToListAsync(cancellationToken);

        return Ok(units);
    }

    [HttpPost("properties/{propertyId:guid}/units")]
    public async Task<ActionResult<UnitResponse>> CreateUnit(
        Guid propertyId,
        CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var ownsProperty = await _dbContext.Properties.AnyAsync(
            property => property.Id == propertyId &&
                        property.OwnerId == ownerId,
            cancellationToken);

        if (!ownsProperty)
            return NotFound();

        var unit = new Unit
        {
            PropertyId = propertyId,
            NameOrNumber = request.NameOrNumber.Trim(),
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            Status = UnitStatus.Available
        };

        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetUnit),
            new { id = unit.Id },
            ToResponse(unit));
    }

    [HttpGet("units/{id:guid}")]
    public async Task<ActionResult<UnitResponse>> GetUnit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var unit = await _dbContext.Units
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id &&
                        item.Property.OwnerId == ownerId,
                cancellationToken);

        return unit is null ? NotFound() : Ok(ToResponse(unit));
    }

    [HttpPut("units/{id:guid}")]
    public async Task<ActionResult<UnitResponse>> UpdateUnit(
        Guid id,
        UpdateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var unit = await _dbContext.Units
            .SingleOrDefaultAsync(
                item => item.Id == id &&
                        item.Property.OwnerId == ownerId,
                cancellationToken);

        if (unit is null)
            return NotFound();

        if (unit.Status == UnitStatus.Occupied ||
            unit.Status == UnitStatus.Archived)
        {
            return Conflict("Occupied or archived units cannot be updated.");
        }

        unit.NameOrNumber = request.NameOrNumber.Trim();
        unit.Bedrooms = request.Bedrooms;
        unit.Bathrooms = request.Bathrooms;
        unit.MonthlyRent = request.MonthlyRent;
        unit.SecurityDeposit = request.SecurityDeposit;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(unit));
    }

    [HttpPost("units/{id:guid}/mark-unavailable")]
    public async Task<ActionResult<UnitResponse>> MarkUnavailable(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var unit = await _dbContext.Units
            .SingleOrDefaultAsync(
                item => item.Id == id &&
                        item.Property.OwnerId == ownerId,
                cancellationToken);

        if (unit is null)
            return NotFound();

        if (unit.Status == UnitStatus.Occupied)
            return Conflict("Occupied units cannot be marked unavailable.");

        if (unit.Status == UnitStatus.Archived)
            return Conflict("Archived units cannot be changed.");

        unit.Status = UnitStatus.Unavailable;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(unit));
    }

    [HttpPost("units/{id:guid}/archive")]
    public async Task<ActionResult<UnitResponse>> ArchiveUnit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var unit = await _dbContext.Units
            .SingleOrDefaultAsync(
                item => item.Id == id &&
                        item.Property.OwnerId == ownerId,
                cancellationToken);

        if (unit is null)
            return NotFound();

        if (unit.Status == UnitStatus.Occupied)
            return Conflict("Occupied units cannot be archived.");

        unit.Status = UnitStatus.Archived;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(unit));
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static UnitResponse ToResponse(Unit unit) =>
        new(
            unit.Id,
            unit.PropertyId,
            unit.NameOrNumber,
            unit.Bedrooms,
            unit.Bathrooms,
            unit.MonthlyRent,
            unit.SecurityDeposit,
            unit.Status);

}
