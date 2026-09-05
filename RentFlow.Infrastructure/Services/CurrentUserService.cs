using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RentFlow.Application.Interfaces;

namespace RentFlow.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? CurrentUser =>
        _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        CurrentUser?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email =>
        CurrentUser?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated =>
        CurrentUser?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) =>
        CurrentUser?.IsInRole(role) == true;
}
