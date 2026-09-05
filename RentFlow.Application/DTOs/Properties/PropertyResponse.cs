using RentFlow.Domain.Entities;

namespace RentFlow.Application.DTOs.Properties;

public sealed record PropertyResponse(
    Guid Id,
    string Name,
    string Description,
    string PropertyType,
    string Address,
    string City,
    string Amenities,
    PropertyStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
