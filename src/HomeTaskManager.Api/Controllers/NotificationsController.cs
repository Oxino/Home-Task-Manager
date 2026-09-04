using HomeTaskManager.Api;
using HomeTaskManager.Application.UseCases.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

/// <summary>The caller's own in-app notification center: list (paged), mark read, and prune read items.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return Ok(await useCase.ListPageAsync(User.GetCurrentUserId(), Math.Max(1, page), Math.Clamp(pageSize, 1, 50), cancellationToken));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<NotificationDto>> SetRead(Guid id, [FromBody] bool isRead, CancellationToken cancellationToken)
    {
        try { return Ok(await useCase.SetReadAsync(new SetNotificationReadCommand(id, User.GetCurrentUserId(), isRead), cancellationToken)); }
        catch (Exception exception) when (exception is Domain.Exceptions.DomainValidationException or Application.Exceptions.ForbiddenOperationException) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken cancellationToken) { await useCase.MarkAllReadAsync(User.GetCurrentUserId(), cancellationToken); return NoContent(); }

    [HttpDelete("read")]
    public async Task<IActionResult> DeleteRead(CancellationToken cancellationToken) { await useCase.DeleteReadAsync(User.GetCurrentUserId(), cancellationToken); return NoContent(); }
}