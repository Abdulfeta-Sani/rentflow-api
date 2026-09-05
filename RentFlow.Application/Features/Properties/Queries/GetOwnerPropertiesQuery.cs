using MediatR;
using RentFlow.Application.DTOs.Properties;

namespace RentFlow.Application.Features.Properties.Queries;

public sealed record GetOwnerPropertiesQuery(
    string OwnerId) : IRequest<IReadOnlyList<PropertyResponse>>;
