using HomeTaskManager.Api.Contracts.Assignments;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/assignments")]
public class AssignmentsController(IRunAssignmentEngineUseCase runAssignmentEngineUseCase) : ControllerBase
{
    [HttpPost("run")]
    public async Task<ActionResult<RunAssignmentEngineResult>> Run(RunAssignmentEngineRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RunAssignmentEngineCommand(request.Year, request.WeekNumber, request.TriggeredByUserId);
            var result = await runAssignmentEngineUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(result);
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
