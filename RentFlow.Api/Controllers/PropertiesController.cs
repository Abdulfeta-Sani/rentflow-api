using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/properties")]
public sealed class PropertiesController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;

    public PropertiesController(
        RentFlowDbContext dbContext,
        IAuthorizationService authorizationService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
    }

    [AllowAnonymous]
    [HttpGet("published")]
    public async Task<ActionResult<IReadOnlyList<Property>>> GetPublished(
        CancellationToken cancellationToken)
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.Status == PropertyStatus.Published)
            .OrderByDescending(property => property.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(properties);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Property>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id &&
                             candidate.Status == PropertyStatus.Published,
                cancellationToken);

        return property is null ? NotFound() : Ok(property);
    }

    [Authorize]
    [HttpGet("{id:guid}/owner-view")]
    public async Task<ActionResult<Property>> GetOwnerProperty(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.PropertyOwner);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var property = await _dbContext.Properties
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        return property is null ? NotFound() : Ok(property);
    }

    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    [HttpPost]
    public async Task<ActionResult<Property>> Create(
        CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
            return Unauthorized();

        var property = new Property
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            PropertyType = request.PropertyType.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Amenities = request.Amenities.Trim(),
            Status = PropertyStatus.Draft
        };

        _dbContext.Properties.Add(property);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = property.Id },
            property);
    }

    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<Property>> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.PropertyOwner);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var property = await _dbContext.Properties
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (property is null)
            return NotFound();

        if (property.Status == PropertyStatus.Published)
            return Ok(property);

        if (property.Status != PropertyStatus.Draft)
            return Conflict("Only draft properties can be published.");

        property.Status = PropertyStatus.Published;
        property.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(property);
    }
}

public sealed class CreatePropertyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Amenities { get; set; } = string.Empty;
}
