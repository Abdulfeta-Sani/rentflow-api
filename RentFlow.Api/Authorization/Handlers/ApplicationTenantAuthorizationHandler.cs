using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Requirements;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Authorization.Handlers;

public sealed class ApplicationTenantAuthorizationHandler
    : AuthorizationHandler<ApplicationTenantRequirement, Guid>
{
    private readonly RentFlowDbContext _dbContext;

    public ApplicationTenantAuthorizationHandler(RentFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApplicationTenantRequirement requirement,
        Guid applicationId)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        if (await _dbContext.RentalApplications.AnyAsync(
                application => application.Id == applicationId &&
                               application.TenantId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
