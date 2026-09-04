using Microsoft.AspNetCore.Identity;

namespace RentFlow.Infrastructure.Identity;

public enum AccountStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Deactivated = 4
}

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}