using HomeTaskManager.Application.UseCases.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

/// <summary>Admin-only read endpoints backing the analytics dashboard (weekly/monthly load, completion rate, load distribution over time).</summary>
[ApiController]
[Route("api/analytics")]
[Authorize]
public sealed class AnalyticsController(
    IGetWeekStatisticsUseCase weekStatisticsUseCase,
    IGetMonthStatisticsUseCase monthStatisticsUseCase,
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

    [HttpGet("month/{year:int}/{month:int}")]
    public async Task<ActionResult<object>> GetMonthStatistics(int year, int month, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        if (month is < 1 or > 12) return BadRequest(new { error = "Month must be between 1 and 12." });
        var stats = await monthStatisticsUseCase.ExecuteAsync(year, month, cancellationToken);
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
