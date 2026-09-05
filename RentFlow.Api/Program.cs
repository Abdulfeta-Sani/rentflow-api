using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Text;
using RentFlow.Api.Authorization.Handlers;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Api.Authorization.Requirements;
using RentFlow.Application.Interfaces;
using RentFlow.Infrastructure.Identity;
using RentFlow.Infrastructure.Persistence;
using RentFlow.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddDbContext<RentFlowDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("RentFlowDatabase");
    options.UseNpgsql(connectionString);
});

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<RentFlowDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "RentFlowAuth";
    options.DefaultChallengeScheme = "RentFlowAuth";
})
.AddJwtBearer(options =>
{
    var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
})
.AddCookie("RentFlowCookie", options =>
{
    options.Cookie.Name = "rentflow.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddPolicyScheme("RentFlowAuth", "JWT or cookie authentication", options =>
{
    options.ForwardDefaultSelector = context =>
        context.Request.Headers.ContainsKey("Authorization")
            ? JwtBearerDefaults.AuthenticationScheme
            : "RentFlowCookie";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin")
              .AddRequirements(new ActiveUserRequirement()));
    options.AddPolicy(AuthorizationPolicies.OwnerOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Owner")
              .AddRequirements(new ActiveUserRequirement()));
    options.AddPolicy(AuthorizationPolicies.TenantOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Tenant")
              .AddRequirements(new ActiveUserRequirement()));
    options.AddPolicy(AuthorizationPolicies.ActiveUser, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new ActiveUserRequirement()));
    options.AddPolicy(AuthorizationPolicies.PropertyOwner, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Owner")
              .AddRequirements(new PropertyOwnerRequirement()));
    options.AddPolicy(AuthorizationPolicies.UnitOwner, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Owner")
              .AddRequirements(new UnitOwnerRequirement()));
    options.AddPolicy(AuthorizationPolicies.ApplicationOwner, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Owner")
              .AddRequirements(new ApplicationOwnerRequirement()));
    options.AddPolicy(AuthorizationPolicies.ApplicationTenant, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Tenant")
              .AddRequirements(new ApplicationTenantRequirement()));
    options.AddPolicy(AuthorizationPolicies.TenancyTenant, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Tenant")
              .AddRequirements(new TenancyTenantRequirement()));
});
builder.Services.AddScoped<ITokenService, TokenServices>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PropertyOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, UnitOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ApplicationOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ApplicationTenantAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenancyTenantAuthorizationHandler>();
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

await DataSeeder.SeedAsync(app.Services);

app.MapControllers();

app.Run();