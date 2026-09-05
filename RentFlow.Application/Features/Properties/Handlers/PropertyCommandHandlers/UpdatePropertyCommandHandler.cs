using MediatR;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Commands;
using RentFlow.Application.Interfaces;

namespace RentFlow.Application.Features.Properties.Handlers.PropertyCommandHandlers;

public sealed class UpdatePropertyCommandHandler
    : IRequestHandler<UpdatePropertyCommand, PropertyResponse?>
{
    private readonly IPropertyService _propertyService;

    public UpdatePropertyCommandHandler(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    public Task<PropertyResponse?> Handle(
        UpdatePropertyCommand request,
        CancellationToken cancellationToken) =>
        _propertyService.UpdatePropertyAsync(
            request.OwnerId,
            request.PropertyId,
            request.Request,
            cancellationToken);
}
