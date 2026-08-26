using HomeTaskManager.Application.UseCases.Audit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController(IGetAuditLogsForEntityUseCase getAuditLogsForEntityUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> GetForEntity(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action,
        CancellationToken cancellationToken)
    {
        if ((!from.HasValue || !to.HasValue) && entityType is null && !entityId.HasValue)
        {
            var page = Request.Query.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage) ? parsedPage : 1;
            var pageSize = Request.Query.TryGetValue("pageSize", out var sizeValue) && int.TryParse(sizeValue, out var parsedSize) ? parsedSize : 10;
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
            return Ok(await getAuditLogsForEntityUseCase.ExecutePageAsync(User.IsInRole("Admin") ? null : User.GetCurrentUserId(), page, pageSize, cancellationToken));
        }
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
