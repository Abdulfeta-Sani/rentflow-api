using MediatR;
using RentFlow.Application.DTOs.Properties;

namespace RentFlow.Application.Features.Properties.Commands;

public sealed record PublishPropertyCommand(
    string OwnerId,
    Guid PropertyId) : IRequest<PropertyResponse?>;
