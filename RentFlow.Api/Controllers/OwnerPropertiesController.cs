using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentFlow.Api.Authorization.Policies;
using RentFlow.Application.DTOs.Properties;
using RentFlow.Application.Features.Properties.Commands;
using RentFlow.Application.Features.Properties.Queries;

namespace RentFlow.Api.Controllers;

[ApiController]
[Route("api/v1/owner/properties")]
[Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
public sealed class OwnerPropertiesController : ControllerBase
{
    private readonly ISender _sender;

    public OwnerPropertiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyResponse>>> GetProperties(
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        return Ok(await _sender.Send(
            new GetOwnerPropertiesQuery(ownerId),
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PropertyResponse>> CreateProperty(
        CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var property = await _sender.Send(
            new CreatePropertyCommand(ownerId, request),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetProperty),
            new { id = property.Id },
            property);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDetailResponse>> GetProperty(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var property = await _sender.Send(
            new GetOwnerPropertyQuery(ownerId, id),
            cancellationToken);

        return property is null ? NotFound() : Ok(property);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyResponse>> UpdateProperty(
        Guid id,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        try
        {
            var property = await _sender.Send(
                new UpdatePropertyCommand(ownerId, id, request),
                cancellationToken);

            return property is null ? NotFound() : Ok(property);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<PropertyResponse>> PublishProperty(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        try
        {
            var property = await _sender.Send(
                new PublishPropertyCommand(ownerId, id),
                cancellationToken);

            return property is null ? NotFound() : Ok(property);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<PropertyResponse>> ArchiveProperty(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        if (ownerId is null)
            return Unauthorized();

        var property = await _sender.Send(
            new ArchivePropertyCommand(ownerId, id),
            cancellationToken);

        return property is null ? NotFound() : Ok(property);
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);
}
