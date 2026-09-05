using MediatR;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Queries;
using RentFlow.Application.Interfaces;

namespace RentFlow.Application.Features.Properties.Handlers.PropertyQueryHandlers;

public sealed class GetOwnerPropertiesQueryHandler
    : IRequestHandler<GetOwnerPropertiesQuery, IReadOnlyList<PropertyResponse>>
{
    private readonly IPropertyService _propertyService;

    public GetOwnerPropertiesQueryHandler(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    public Task<IReadOnlyList<PropertyResponse>> Handle(
        GetOwnerPropertiesQuery request,
        CancellationToken cancellationToken) =>
        _propertyService.GetOwnerPropertiesAsync(
            request.OwnerId,
            cancellationToken);
}
