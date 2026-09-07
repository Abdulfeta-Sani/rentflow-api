using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Application.DTOs;
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
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PropertyResponse>>> Get(
        [FromQuery] string? city,
        [FromQuery] string? propertyType,
        [FromQuery] decimal? minimumRent,
        [FromQuery] decimal? maximumRent,
        [FromQuery] int? bedrooms,
        [FromQuery] PagedRequest? request,
        CancellationToken cancellationToken)
    {
        var pageRequest = request ?? new PagedRequest();
        var page = pageRequest.Page;
        var pageSize = pageRequest.PageSize;

        var query = _dbContext.Properties
            .AsNoTracking()
            .Where(property =>
                property.Status == PropertyStatus.Published &&
                property.Units.Any(unit => unit.Status == UnitStatus.Available));

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(property =>
                property.City.ToLower() == city.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            query = query.Where(property =>
                property.PropertyType.ToLower() == propertyType.Trim().ToLower());
        }

        if (minimumRent.HasValue)
        {
            query = query.Where(property =>
                property.Units.Any(unit =>
                    unit.Status == UnitStatus.Available &&
                    unit.MonthlyRent >= minimumRent.Value));
        }

        if (maximumRent.HasValue)
        {
            query = query.Where(property =>
                property.Units.Any(unit =>
                    unit.Status == UnitStatus.Available &&
                    unit.MonthlyRent <= maximumRent.Value));
        }

        if (bedrooms.HasValue)
        {
            query = query.Where(property =>
                property.Units.Any(unit =>
                    unit.Status == UnitStatus.Available &&
                    unit.Bedrooms == bedrooms.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(property => property.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        var response = new PagedResponse<PropertyResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };

        return Ok(response);
    }


    [AllowAnonymous]
    [HttpGet("published")]
    public Task<ActionResult<PagedResponse<PropertyResponse>>> GetPublished(
        [FromQuery] string? city,
        [FromQuery] string? propertyType,
        [FromQuery] decimal? minimumRent,
        [FromQuery] decimal? maximumRent,
        [FromQuery] int? bedrooms,
        [FromQuery] PagedRequest? request,
        CancellationToken cancellationToken) =>
        Get(city, propertyType, minimumRent, maximumRent, bedrooms, request, cancellationToken);

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
