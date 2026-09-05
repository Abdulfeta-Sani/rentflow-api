using MediatR;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Queries;
using RentFlow.Application.Interfaces;

namespace RentFlow.Application.Features.Properties.Handlers.PropertyQueryHandlers;

public sealed class GetOwnerPropertyQueryHandler
    : IRequestHandler<GetOwnerPropertyQuery, PropertyDetailResponse?>
{
    private readonly IPropertyService _propertyService;

    public GetOwnerPropertyQueryHandler(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    public Task<PropertyDetailResponse?> Handle(
        GetOwnerPropertyQuery request,
        CancellationToken cancellationToken) =>
        _propertyService.GetOwnerPropertyAsync(
            request.OwnerId,
            request.PropertyId,
            cancellationToken);
}
