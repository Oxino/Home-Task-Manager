using HomeTaskManager.Application.UseCases.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public sealed class AnalyticsController(
    IGetWeekStatisticsUseCase weekStatisticsUseCase,
    IGetCompletionStatisticsUseCase completionStatisticsUseCase,
    IGetUserLoadDistributionUseCase loadDistributionUseCase) : ControllerBase
{
    [HttpGet("week/{weekId}")]
    public async Task<ActionResult<object>> GetWeekStatistics(Guid weekId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var stats = await weekStatisticsUseCase.ExecuteAsync(weekId, cancellationToken);
        return Ok(stats);
    }

    [HttpGet("completion")]
    public async Task<ActionResult<object>> GetCompletionStatistics(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var stats = await completionStatisticsUseCase.ExecuteAsync(cancellationToken);
        return Ok(stats);
    }

    [HttpGet("load-distribution")]
    public async Task<ActionResult<object>> GetLoadDistribution(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var stats = await loadDistributionUseCase.ExecuteAsync(cancellationToken);
        return Ok(stats);
    }
}
