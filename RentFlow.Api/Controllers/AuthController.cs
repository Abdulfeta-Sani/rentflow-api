using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentFlow.Infrastructure.Identity;
using RentFlow.Infrastructure.Services;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Admin registration is not allowed.");

        if (!request.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase) &&
            !request.Role.Equals("Tenant", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Role must be Owner or Tenant.");
        }

        var email = request.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
            return Conflict("User already exists.");

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            AccountStatus = AccountStatus.Pending
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role.Trim());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors);
        }

        return Ok(new
        {
            message = "Registration successful. Account is pending activation."
        });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Unauthorized();

        if (user.AccountStatus != AccountStatus.Active)
            return Unauthorized("Account is not active.");

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized("Account is locked.");

        if (!result.Succeeded)
            return Unauthorized();

        var tokens = await _tokenService.IssueAsync(user, cancellationToken);
        await HttpContext.SignInAsync(
            "RentFlowCookie",
            await CreatePrincipalAsync(user));

        return Ok(new
        {
            accessToken = tokens.AccessToken,
            refreshToken = tokens.RefreshToken,
            user = await ToUserResponse(user)
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required.");

        var tokens = await _tokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);

        return tokens is null
            ? Unauthorized("Refresh token is invalid, expired, or revoked.")
            : Ok(new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken
            });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await _tokenService.RevokeAsync(
                request.RefreshToken,
                cancellationToken);
        }

        await HttpContext.SignOutAsync("RentFlowCookie");
        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        return user is null
            ? NotFound()
            : Ok(await ToUserResponse(user));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? Ok(await ToUserResponse(user))
            : BadRequest(result.Errors);
    }

    private async Task<object> ToUserResponse(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new
        {
            id = user.Id,
            email = user.Email,
            phoneNumber = user.PhoneNumber,
            firstName = user.FirstName,
            lastName = user.LastName,
            role = roles.FirstOrDefault(),
            accountStatus = user.AccountStatus.ToString()
        };
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("accountStatus", user.AccountStatus.ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, "RentFlowCookie"));
    }
}

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Tenant";
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
