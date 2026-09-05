using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Requirements;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Authorization.Handlers;

public sealed class TenancyTenantAuthorizationHandler
    : AuthorizationHandler<TenancyTenantRequirement, Guid>
{
    private readonly RentFlowDbContext _dbContext;

    public TenancyTenantAuthorizationHandler(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenancyTenantRequirement requirement,
        Guid tenancyId)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        if (await _dbContext.Tenancies.AnyAsync(
                tenancy => tenancy.Id == tenancyId &&
                           tenancy.TenantId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
