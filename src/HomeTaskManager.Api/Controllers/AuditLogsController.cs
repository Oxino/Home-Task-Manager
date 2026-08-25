using HomeTaskManager.Application.UseCases.Audit;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController(IGetAuditLogsForEntityUseCase getAuditLogsForEntityUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetForEntity(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        CancellationToken cancellationToken)
    {
        var entries = entityType is not null && entityId.HasValue
            ? await getAuditLogsForEntityUseCase.ExecuteAsync(entityType, entityId.Value, cancellationToken)
            : await getAuditLogsForEntityUseCase.ExecuteAllAsync(cancellationToken);
        return Ok(entries);
    }
}
