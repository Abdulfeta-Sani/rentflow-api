using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;
using RentFlow.Infrastructure.Identity;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/rental-applications")]
public sealed class RentalApplicationsController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly UserManager<AppUser> _userManager;

    public RentalApplicationsController(
        RentFlowDbContext dbContext,
        IAuthorizationService authorizationService,
        UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _userManager = userManager;
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOnly)]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<RentalApplicationResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var tenantId = GetCurrentUserId();
        if (tenantId is null)
            return Unauthorized();

        var applications = await _dbContext.RentalApplications
            .AsNoTracking()
            .Include(application => application.Unit)
            .ThenInclude(unit => unit.Property)
            .Where(application => application.TenantId == tenantId)
            .OrderByDescending(application => application.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        var users = await GetUsers(applications, cancellationToken);
        return Ok(applications.Select(application => ToResponse(
            application, users.GetValueOrDefault(application.TenantId), users.GetValueOrDefault(application.Unit.Property.OwnerId))).ToList());
    }

    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    [HttpGet("owner")]
    public async Task<ActionResult<IReadOnlyList<RentalApplicationResponse>>> GetOwnerApplications(
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var applications = await _dbContext.RentalApplications
            .AsNoTracking()
            .Include(application => application.Unit)
            .ThenInclude(unit => unit.Property)
            .Where(application => application.Unit.Property.OwnerId == ownerId)
            .OrderByDescending(application => application.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        var users = await GetUsers(applications, cancellationToken);
        return Ok(applications.Select(application => ToResponse(
            application, users.GetValueOrDefault(application.TenantId), users.GetValueOrDefault(application.Unit.Property.OwnerId))).ToList());
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOnly)]
    [HttpPost]
    public async Task<ActionResult<RentalApplicationResponse>> Create(
        CreateRentalApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = GetCurrentUserId();
        if (tenantId is null)
            return Unauthorized();

        var unit = await _dbContext.Units
            .Include(candidate => candidate.Property)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.UnitId &&
                             candidate.Property.Status == PropertyStatus.Published,
                cancellationToken);

        if (unit is null)
            return NotFound("Published unit not found.");

        if (unit.Status != UnitStatus.Available)
            return Conflict("This unit is not available.");

        var alreadyApplied = await _dbContext.RentalApplications.AnyAsync(
            application => application.UnitId == request.UnitId &&
                           application.TenantId == tenantId &&
                           application.Status != RentalApplicationStatus.Rejected &&
                           application.Status != RentalApplicationStatus.Withdrawn,
            cancellationToken);

        if (alreadyApplied)
            return Conflict("You already have an active application for this unit.");

        var application = new RentalApplication
        {
            TenantId = tenantId,
            UnitId = request.UnitId,
            Unit = unit,
            EmploymentInformation = request.EmploymentInformation.Trim(),
            NumberOfOccupants = request.NumberOfOccupants,
            PreferredMoveInDate = request.PreferredMoveInDate,
            Message = request.Message.Trim()
        };

        _dbContext.RentalApplications.Add(application);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTenantApplication),
            new { id = application.Id },
            ToResponse(application, await _userManager.FindByIdAsync(tenantId), await _userManager.FindByIdAsync(unit.Property.OwnerId)));
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOnly)]
    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetCurrentUserId();
        if (tenantId is null)
            return Unauthorized();

        var application = await _dbContext.RentalApplications
            .SingleOrDefaultAsync(item => item.Id == id && item.TenantId == tenantId, cancellationToken);
        if (application is null)
            return NotFound();
        if (application.Status is not (RentalApplicationStatus.Submitted or RentalApplicationStatus.UnderReview))
            return Conflict("Only submitted or under-review applications can be withdrawn.");

        application.Status = RentalApplicationStatus.Withdrawn;
        application.ReviewedAtUtc = DateTime.UtcNow;
        application.DecisionReason = "Withdrawn by tenant.";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        ReviewAsync(id, RentalApplicationStatus.Approved, null, cancellationToken);

    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(
        Guid id,
        ReviewRequest request,
        CancellationToken cancellationToken) =>
        ReviewAsync(id, RentalApplicationStatus.Rejected, request.DecisionReason, cancellationToken);

    [Authorize]
    [HttpGet("{id:guid}/owner-view")]
    public async Task<ActionResult<RentalApplicationResponse>> GetOwnerApplication(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User, id, AuthorizationPolicies.ApplicationOwner);

        if (!authorizationResult.Succeeded)
            return Forbid();

        return await GetApplicationResponse(id, cancellationToken);
    }

    [Authorize]
    [HttpGet("{id:guid}/tenant-view")]
    public async Task<ActionResult<RentalApplicationResponse>> GetTenantApplication(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User, id, AuthorizationPolicies.ApplicationTenant);

        if (!authorizationResult.Succeeded)
            return Forbid();

        return await GetApplicationResponse(id, cancellationToken);
    }

    private async Task<ActionResult<RentalApplicationResponse>> GetApplicationResponse(
        Guid id,
        CancellationToken cancellationToken)
    {
        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .Include(item => item.Unit)
            .ThenInclude(unit => unit.Property)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
            return NotFound();

        var tenant = await _userManager.FindByIdAsync(application.TenantId);
        var owner = await _userManager.FindByIdAsync(application.Unit.Property.OwnerId);
        return Ok(ToResponse(application, tenant, owner));
    }

    private async Task<Dictionary<string, AppUser>> GetUsers(
        IReadOnlyCollection<RentalApplication> applications,
        CancellationToken cancellationToken)
    {
        var ids = applications.SelectMany(application => new[]
            { application.TenantId, application.Unit.Property.OwnerId }).Distinct().ToList();
        return await _userManager.Users
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
    }

    private async Task<IActionResult> ReviewAsync(
        Guid id,
        RentalApplicationStatus status,
        string? decisionReason,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var application = await _dbContext.RentalApplications
            .Include(item => item.Unit)
            .ThenInclude(unit => unit.Property)
            .SingleOrDefaultAsync(
                item => item.Id == id && item.Unit.Property.OwnerId == ownerId,
                cancellationToken);

        if (application is null)
            return NotFound();

        if (application.Status is RentalApplicationStatus.Approved or RentalApplicationStatus.Rejected)
            return Conflict("This application has already been reviewed.");

        if (status == RentalApplicationStatus.Approved)
        {
            if (application.Unit.Status != UnitStatus.Available)
                return Conflict("The unit is no longer available.");

            application.Unit.Status = UnitStatus.Occupied;
            _dbContext.Tenancies.Add(new Tenancy
            {
                ApplicationId = application.Id,
                UnitId = application.UnitId,
                TenantId = application.TenantId,
                OwnerId = ownerId,
                StartDate = application.PreferredMoveInDate,
                EndDate = application.PreferredMoveInDate.AddYears(1),
                MonthlyRent = application.Unit.MonthlyRent,
                SecurityDeposit = application.Unit.SecurityDeposit,
                ActivatedAtUtc = DateTime.UtcNow
            });
        }

        application.Status = status;
        application.DecisionReason = string.IsNullOrWhiteSpace(decisionReason)
            ? null
            : decisionReason.Trim();
        application.ReviewedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tenant = await _userManager.FindByIdAsync(application.TenantId);
        var owner = await _userManager.FindByIdAsync(application.Unit.Property.OwnerId);
        return Ok(ToResponse(application, tenant, owner));
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static RentalApplicationResponse ToResponse(
        RentalApplication application,
        AppUser? tenant,
        AppUser? owner) =>
        new(
            application.Id,
            application.TenantId,
            application.UnitId,
            application.Unit.NameOrNumber,
            application.Unit.Property.Name,
            application.EmploymentInformation,
            application.NumberOfOccupants,
            application.PreferredMoveInDate,
            application.Message,
            application.Status,
            application.SubmittedAtUtc,
            application.ReviewedAtUtc,
            application.DecisionReason)
        {
            TenantFullName = tenant is null ? null : $"{tenant.FirstName} {tenant.LastName}".Trim(),
            TenantPhoneNumber = tenant?.PhoneNumber,
            TenantEmail = tenant?.Email,
            OwnerFullName = owner is null ? null : $"{owner.FirstName} {owner.LastName}".Trim(),
            OwnerPhoneNumber = owner?.PhoneNumber,
            PropertyDescription = application.Unit.Property.Description,
            PropertyType = application.Unit.Property.PropertyType,
            PropertyAddress = application.Unit.Property.Address,
            PropertyCity = application.Unit.Property.City,
            PropertyStatus = application.Unit.Property.Status
        };
}

public sealed class CreateRentalApplicationRequest
{
    public Guid UnitId { get; set; }
    public string EmploymentInformation { get; set; } = string.Empty;
    public int NumberOfOccupants { get; set; }
    public DateOnly PreferredMoveInDate { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ReviewRequest
{
    public string? DecisionReason { get; set; }
}

public sealed record RentalApplicationResponse(
    Guid Id,
    string TenantId,
    Guid UnitId,
    string UnitNameOrNumber,
    string PropertyName,
    string EmploymentInformation,
    int NumberOfOccupants,
    DateOnly PreferredMoveInDate,
    string Message,
    RentalApplicationStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionReason)
{
    public string? TenantFullName { get; init; }
    public string? TenantPhoneNumber { get; init; }
    public string? TenantEmail { get; init; }
    public string? OwnerFullName { get; init; }
    public string? OwnerPhoneNumber { get; init; }
    public string PropertyDescription { get; init; } = string.Empty;
    public string PropertyType { get; init; } = string.Empty;
    public string PropertyAddress { get; init; } = string.Empty;
    public string PropertyCity { get; init; } = string.Empty;
    public PropertyStatus PropertyStatus { get; init; }
}
