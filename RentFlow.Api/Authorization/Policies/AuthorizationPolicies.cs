namespace RentFlow.Api.Authorization.Policies;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string OwnerOnly = "OwnerOnly";
    public const string TenantOnly = "TenantOnly";
    public const string ActiveUser = "ActiveUser";
    public const string PropertyOwner = "PropertyOwner";
    public const string UnitOwner = "UnitOwner";
    public const string ApplicationOwner = "ApplicationOwner";
    public const string ApplicationTenant = "ApplicationTenant";
    public const string TenancyTenant = "TenancyTenant";
}
