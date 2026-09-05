using MediatR;
using RentFlow.Application.DTOs.Properties;

namespace RentFlow.Application.Features.Properties.Commands;

public sealed record UpdatePropertyCommand(
    string OwnerId,
    Guid PropertyId,
    UpdatePropertyRequest Request) : IRequest<PropertyResponse?>;
