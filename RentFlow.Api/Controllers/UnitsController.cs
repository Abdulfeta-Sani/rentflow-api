using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Persistence;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/units")]
public sealed class UnitsController : ControllerBase
{
    private readonly RentFlowDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;

    public UnitsController(
        RentFlowDbContext dbContext,
        IAuthorizationService authorizationService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
    }

    [Authorize]
    [HttpGet("{id:guid}/owner-view")]
    public async Task<ActionResult<Unit>> GetOwnerUnit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            id,
            AuthorizationPolicies.UnitOwner);

        if (!authorizationResult.Succeeded)
            return Forbid();

        var unit = await _dbContext.Units
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        return unit is null ? NotFound() : Ok(unit);
    }
}
