using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Requirements;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Authorization.Handlers;

public sealed class PropertyOwnerAuthorizationHandler
    : AuthorizationHandler<PropertyOwnerRequirement, Guid>
{
    private readonly RentFlowDbContext _dbContext;

    public PropertyOwnerAuthorizationHandler(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PropertyOwnerRequirement requirement,
        Guid propertyId)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        if (await _dbContext.Properties.AnyAsync(
                property => property.Id == propertyId &&
                            property.OwnerId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
