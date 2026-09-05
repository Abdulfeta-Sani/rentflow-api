using MediatR;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Commands;
using RentFlow.Application.Interfaces;

namespace RentFlow.Application.Features.Properties.Handlers.PropertyCommandHandlers;

public sealed class ArchivePropertyCommandHandler
    : IRequestHandler<ArchivePropertyCommand, PropertyResponse?>
{
    private readonly IPropertyService _propertyService;

    public ArchivePropertyCommandHandler(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    public Task<PropertyResponse?> Handle(
        ArchivePropertyCommand request,
        CancellationToken cancellationToken) =>
        _propertyService.ArchivePropertyAsync(
            request.OwnerId,
            request.PropertyId,
            cancellationToken);
}
