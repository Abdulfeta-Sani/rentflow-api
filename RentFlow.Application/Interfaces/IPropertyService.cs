using RentFlow.Application.DTOs.Properties;

namespace RentFlow.Application.Interfaces;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertyResponse>> GetOwnerPropertiesAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<PropertyDetailResponse?> GetOwnerPropertyAsync(
        string ownerId,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<PropertyResponse> CreatePropertyAsync(
        string ownerId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken = default);

    Task<PropertyResponse?> UpdatePropertyAsync(
        string ownerId,
        Guid propertyId,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken = default);

    Task<PropertyResponse?> PublishPropertyAsync(
        string ownerId,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<PropertyResponse?> ArchivePropertyAsync(
        string ownerId,
        Guid propertyId,
        CancellationToken cancellationToken = default);
}
