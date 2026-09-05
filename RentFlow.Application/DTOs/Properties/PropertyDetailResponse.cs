using RentFlow.Domain.Entities;

namespace RentFlow.Application.DTOs.Properties;

public sealed record PropertyDetailResponse(
    Guid Id,
    string Name,
    string Description,
    string PropertyType,
    string Address,
    string City,
    string Amenities,
    PropertyStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<PropertyUnitResponse> Units);

public sealed record PropertyUnitResponse(
    Guid Id,
    string NameOrNumber,
    int Bedrooms,
    int Bathrooms,
    decimal MonthlyRent,
    decimal SecurityDeposit,
    UnitStatus Status);
