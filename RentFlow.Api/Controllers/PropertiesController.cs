using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/properties")]
public sealed class PropertiesController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;

    public PropertiesController(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet("published")]
    public async Task<ActionResult<IReadOnlyList<PropertyResponse>>> GetPublished(
        CancellationToken cancellationToken)
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Where(property =>
                property.Status == PropertyStatus.Published &&
                property.Units.Any(unit => unit.Status == UnitStatus.Available))
            .OrderByDescending(property => property.CreatedAtUtc)
            .Select(property => new PropertyResponse(
                property.Id,
                property.Name,
                property.Description,
                property.PropertyType,
                property.Address,
                property.City,
                property.Amenities,
                property.Status,
                property.CreatedAtUtc,
                property.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(properties);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDetailResponse>> GetPublishedById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(item =>
                item.Id == id &&
                item.Status == PropertyStatus.Published &&
                item.Units.Any(unit => unit.Status == UnitStatus.Available))
            .Select(item => new PropertyDetailResponse(
                item.Id,
                item.Name,
                item.Description,
                item.PropertyType,
                item.Address,
                item.City,
                item.Amenities,
                item.Status,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.Units
                    .Where(unit => unit.Status == UnitStatus.Available)
                    .OrderBy(unit => unit.NameOrNumber)
                    .Select(unit => new PropertyUnitResponse(
                        unit.Id,
                        unit.NameOrNumber,
                        unit.Bedrooms,
                        unit.Bathrooms,
                        unit.MonthlyRent,
                        unit.SecurityDeposit,
                        unit.Status))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return property is null ? NotFound() : Ok(property);
    }
}
