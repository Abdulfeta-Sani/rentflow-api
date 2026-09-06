using RentFlow.Domain.Entities;

namespace RentFlow.Application.DTOs.Units;

public sealed record UnitResponse(
    Guid Id,
    Guid PropertyId,
    string NameOrNumber,
    int Bedrooms,
    int Bathrooms,
    decimal MonthlyRent,
    decimal SecurityDeposit,
    UnitStatus Status)
{
    public string Currency => "ETB";
}
