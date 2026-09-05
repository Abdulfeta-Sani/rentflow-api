using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/rental-applications")]
public sealed class RentalApplicationsController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;

    public RentalApplicationsController(
        RentFlowDbContext dbContext,
        IAuthorizationService authorizationService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
    }

    [Authorize]
    [HttpGet("{id:guid}/owner-view")]
    public async Task<ActionResult<RentalApplication>> GetOwnerApplication(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.ApplicationOwner);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        return application is null ? NotFound() : Ok(application);
    }

    [Authorize]
    [HttpGet("{id:guid}/tenant-view")]
    public async Task<ActionResult<RentalApplication>> GetTenantApplication(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.ApplicationTenant);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var application = await _dbContext.RentalApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        return application is null ? NotFound() : Ok(application);
    }
}
