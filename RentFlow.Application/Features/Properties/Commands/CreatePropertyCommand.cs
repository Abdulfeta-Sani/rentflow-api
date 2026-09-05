using MediatR;
using RentFlow.Application.DTOs.Properties;

namespace RentFlow.Application.Features.Properties.Commands;

public sealed record CreatePropertyCommand(
    string OwnerId,
    CreatePropertyRequest Request) : IRequest<PropertyResponse>;
