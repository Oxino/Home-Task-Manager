using HomeTaskManager.Application.UseCases.Audit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;

namespace HomeTaskManager.Api.Controllers;

/// <summary>Exposes the audit trail (BR-033); non-admins are always restricted to their own entries regardless of query filters.</summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController(IGetAuditLogsForEntityUseCase getAuditLogsForEntityUseCase) : ControllerBase
{
    /// <summary>Shortcut for "my own audit history", paged.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<object>> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return Ok(await getAuditLogsForEntityUseCase.ExecutePageAsync(User.GetCurrentUserId(), null, null, null, null, Math.Max(1, page), Math.Clamp(pageSize, 1, 50), cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetForEntity(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action,
        [FromQuery] string? category,
        [FromQuery] string? scope,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        // Default case (no date range, no specific entity): the paged/filterable list used by the audit screen.
        // Non-admins are hard-restricted to their own entries (performerId) no matter what "category"/"scope" is requested.
        if ((!from.HasValue || !to.HasValue) && entityType is null && !entityId.HasValue)
        {
            var page = Request.Query.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage) ? parsedPage : 1;
            var pageSize = Request.Query.TryGetValue("pageSize", out var sizeValue) && int.TryParse(sizeValue, out var parsedSize) ? parsedSize : 10;
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
            var personalScope = scope == "personal";
            return Ok(await getAuditLogsForEntityUseCase.ExecutePageAsync(personalScope || !User.IsInRole("Admin") ? User.GetCurrentUserId() : null, personalScope ? null : category, entityType, action, sort, page, pageSize, cancellationToken));
        }
        // Legacy/specialized query shapes: per-performer, per-period, or per-entity, still admin-gated for anything
        // beyond the caller's own history.
        var entries = !User.IsInRole("Admin")
            ? await getAuditLogsForEntityUseCase.ExecuteForPerformerAsync(User.GetCurrentUserId(), cancellationToken)
            : from.HasValue && to.HasValue && User.IsInRole("Admin")
            ? await getAuditLogsForEntityUseCase.ExecuteForPeriodAsync(from.Value, to.Value, action, cancellationToken)
            : entityType is not null && entityId.HasValue
            ? await getAuditLogsForEntityUseCase.ExecuteAsync(entityType, entityId.Value, cancellationToken)
            : await getAuditLogsForEntityUseCase.ExecuteAllAsync(cancellationToken);
        return Ok(entries);
    }
}
