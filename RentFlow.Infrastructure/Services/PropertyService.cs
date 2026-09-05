using Microsoft.EntityFrameworkCore;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Interfaces;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Infrastructure.Services;

public sealed class PropertyService : IPropertyService
{
    private readonly RentFlowDbContext _dbContext;

    public PropertyService(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PropertyResponse>> GetOwnerPropertiesAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.OwnerId == ownerId)
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
    }

    public async Task<PropertyDetailResponse?> GetOwnerPropertyAsync(
        string ownerId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Include(item => item.Units)
            .SingleOrDefaultAsync(
                item => item.Id == propertyId &&
                        item.OwnerId == ownerId,
                cancellationToken);

        return property is null ? null : ToDetailResponse(property);
    }

    public async Task<PropertyResponse> CreatePropertyAsync(
        string ownerId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        var property = new Property
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            PropertyType = request.PropertyType.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Amenities = request.Amenities.Trim(),
            Status = PropertyStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Properties.Add(property);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(property);
    }

    public async Task<PropertyResponse?> UpdatePropertyAsync(
        string ownerId,
        Guid propertyId,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .SingleOrDefaultAsync(
                item => item.Id == propertyId &&
                        item.OwnerId == ownerId,
                cancellationToken);

        if (property is null)
            return null;

        if (property.Status == PropertyStatus.Archived)
            throw new InvalidOperationException(
                "Archived properties cannot be updated.");

        property.Name = request.Name.Trim();
        property.Description = request.Description.Trim();
        property.PropertyType = request.PropertyType.Trim();
        property.Address = request.Address.Trim();
        property.City = request.City.Trim();
        property.Amenities = request.Amenities.Trim();
        property.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(property);
    }

    public async Task<PropertyResponse?> PublishPropertyAsync(
        string ownerId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .Include(item => item.Units)
            .SingleOrDefaultAsync(
                item => item.Id == propertyId &&
                        item.OwnerId == ownerId,
                cancellationToken);

        if (property is null)
            return null;

        if (property.Status == PropertyStatus.Archived)
            throw new InvalidOperationException(
                "Archived properties cannot be published.");

        if (property.Units.Count == 0)
            throw new InvalidOperationException(
                "A property must have at least one unit before publishing.");

        if (!property.Units.Any(unit => unit.Status == UnitStatus.Available))
            throw new InvalidOperationException(
                "A property must have at least one available unit before publishing.");

        property.Status = PropertyStatus.Published;
        property.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(property);
    }

    public async Task<PropertyResponse?> ArchivePropertyAsync(
        string ownerId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .SingleOrDefaultAsync(
                item => item.Id == propertyId &&
                        item.OwnerId == ownerId,
                cancellationToken);

        if (property is null)
            return null;

        property.Status = PropertyStatus.Archived;
        property.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(property);
    }

    private static PropertyResponse ToResponse(Property property) =>
        new(
            property.Id,
            property.Name,
            property.Description,
            property.PropertyType,
            property.Address,
            property.City,
            property.Amenities,
            property.Status,
            property.CreatedAtUtc,
            property.UpdatedAtUtc);

    private static PropertyDetailResponse ToDetailResponse(Property property) =>
        new(
            property.Id,
            property.Name,
            property.Description,
            property.PropertyType,
            property.Address,
            property.City,
            property.Amenities,
            property.Status,
            property.CreatedAtUtc,
            property.UpdatedAtUtc,
            property.Units
                .OrderBy(unit => unit.NameOrNumber)
                .Select(unit => new PropertyUnitResponse(
                    unit.Id,
                    unit.NameOrNumber,
                    unit.Bedrooms,
                    unit.Bathrooms,
                    unit.MonthlyRent,
                    unit.SecurityDeposit,
                    unit.Status))
                .ToList());
}
