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
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetForEntity(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        CancellationToken cancellationToken)
    {
        var entries = !User.IsInRole("Admin")
            ? await getAuditLogsForEntityUseCase.ExecuteForPerformerAsync(User.GetCurrentUserId(), cancellationToken)
            : entityType is not null && entityId.HasValue
            ? await getAuditLogsForEntityUseCase.ExecuteAsync(entityType, entityId.Value, cancellationToken)
            : await getAuditLogsForEntityUseCase.ExecuteAllAsync(cancellationToken);
        return Ok(entries);
    }
}
