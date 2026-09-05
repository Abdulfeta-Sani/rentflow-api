using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Requirements;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Authorization.Handlers;

public sealed class UnitOwnerAuthorizationHandler
    : AuthorizationHandler<UnitOwnerRequirement, Guid>
{
    private readonly RentFlowDbContext _dbContext;

    public UnitOwnerAuthorizationHandler(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UnitOwnerRequirement requirement,
        Guid unitId)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        if (await _dbContext.Units.AnyAsync(
                unit => unit.Id == unitId &&
                        unit.Property.OwnerId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
