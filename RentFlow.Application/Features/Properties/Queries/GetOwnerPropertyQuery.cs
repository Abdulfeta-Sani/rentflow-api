using MediatR;
using RentFlow.Application.DTOs.Properties;

namespace RentFlow.Application.Features.Properties.Queries;

public sealed record GetOwnerPropertyQuery(
    string OwnerId,
    Guid PropertyId) : IRequest<PropertyDetailResponse?>;
