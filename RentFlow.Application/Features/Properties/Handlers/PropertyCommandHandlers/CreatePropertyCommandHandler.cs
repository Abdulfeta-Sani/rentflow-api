using MediatR;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Commands;
using RentFlow.Application.Interfaces;

namespace RentFlow.Application.Features.Properties.Handlers.PropertyCommandHandlers;

public sealed class CreatePropertyCommandHandler
    : IRequestHandler<CreatePropertyCommand, PropertyResponse>
{
    private readonly IPropertyService _propertyService;

    public CreatePropertyCommandHandler(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    public Task<PropertyResponse> Handle(
        CreatePropertyCommand request,
        CancellationToken cancellationToken) =>
        _propertyService.CreatePropertyAsync(
            request.OwnerId,
            request.Request,
            cancellationToken);
}
