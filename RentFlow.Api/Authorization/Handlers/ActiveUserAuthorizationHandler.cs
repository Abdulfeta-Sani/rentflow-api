using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using RentFlow.Infrastructure.Identity;
using RentFlow.Api.Authorization.Requirements;

namespace RentFlow.Api.Authorization.Handlers;

public sealed class ActiveUserAuthorizationHandler
    : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly UserManager<AppUser> _userManager;

    public ActiveUserAuthorizationHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var user = await _userManager.FindByIdAsync(userId);
        if (user?.AccountStatus == AccountStatus.Active)
            context.Succeed(requirement);
    }
}
