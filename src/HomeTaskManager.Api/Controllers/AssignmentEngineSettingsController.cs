using HomeTaskManager.Api.Contracts.Settings;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Settings;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/settings/assignment-engine")]
public class AssignmentEngineSettingsController(
    IGetAssignmentEngineSettingsUseCase getAssignmentEngineSettingsUseCase,
    IUpdateAssignmentEngineSettingsUseCase updateAssignmentEngineSettingsUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AssignmentEngineSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await getAssignmentEngineSettingsUseCase.ExecuteAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssignmentEngineSettingsDto>> Update(
        UpdateAssignmentEngineSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateAssignmentEngineSettingsCommand(
                User.GetCurrentUserId(),
                request.LowPriorityMultiplier,
                request.NormalPriorityMultiplier,
                request.CriticalPriorityMultiplier,
                request.ReferenceDurationMinutes,
                request.EquityWindowWeeks,
                request.RepetitionPenaltyWeight);

            var settings = await updateAssignmentEngineSettingsUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(settings);
        }
        catch (ForbiddenOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
