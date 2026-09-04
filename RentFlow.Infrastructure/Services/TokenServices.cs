using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Identity;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Infrastructure.Services;

public sealed record TokenPair(string AccessToken, string RefreshToken);

public interface ITokenService
{
    Task<TokenPair> IssueAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<TokenPair?> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public sealed class TokenServices : ITokenService
{
    private readonly RentFlowDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public TokenServices(RentFlowDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<TokenPair> IssueAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesAsync(user.Id, cancellationToken);
        var rawRefreshToken = CreateRefreshToken();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays())
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TokenPair(CreateAccessToken(user, roles), rawRefreshToken);
    }

    public async Task<TokenPair?> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var current = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == HashToken(refreshToken),
                cancellationToken);

        if (current is null)
            return null;

        if (current.IsRevoked || current.IsExpired)
        {
            await RevokeFamilyAsync(current.TokenFamilyId, cancellationToken);
            return null;
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == current.UserId,
                cancellationToken);

        if (user is null || user.AccountStatus != AccountStatus.Active)
            return null;

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        current.RevokedAtUtc = DateTime.UtcNow;

        var newRawRefreshToken = CreateRefreshToken();
        var newTokenHash = HashToken(newRawRefreshToken);
        current.ReplacedByTokenHash = newTokenHash;

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            TokenFamilyId = current.TokenFamilyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays())
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var roles = await GetRolesAsync(user.Id, cancellationToken);
        return new TokenPair(CreateAccessToken(user, roles), newRawRefreshToken);
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var current = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == HashToken(refreshToken),
                cancellationToken);

        if (current is not null)
            await RevokeFamilyAsync(current.TokenFamilyId, cancellationToken);
    }

    private async Task RevokeFamilyAsync(
        Guid tokenFamilyId,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(token =>
                token.TokenFamilyId == tokenFamilyId &&
                token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.RevokedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<string>> GetRolesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(
                _dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name!)
            .ToListAsync(cancellationToken);
    }

    private string CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("accountStatus", user.AccountStatus.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetRefreshTokenLifetimeDays() =>
        _configuration.GetValue("Jwt:RefreshTokenLifetimeDays", 7);

    private static string CreateRefreshToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
