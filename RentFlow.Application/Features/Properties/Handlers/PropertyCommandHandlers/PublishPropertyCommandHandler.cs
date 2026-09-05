using MediatR;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Commands;
using RentFlow.Application.Interfaces;

namespace RentFlow.Application.Features.Properties.Handlers.PropertyCommandHandlers;

public sealed class PublishPropertyCommandHandler
    : IRequestHandler<PublishPropertyCommand, PropertyResponse?>
{
    private readonly IPropertyService _propertyService;

    public PublishPropertyCommandHandler(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    public Task<PropertyResponse?> Handle(
        PublishPropertyCommand request,
        CancellationToken cancellationToken) =>
        _propertyService.PublishPropertyAsync(
            request.OwnerId,
            request.PropertyId,
            cancellationToken);
}
