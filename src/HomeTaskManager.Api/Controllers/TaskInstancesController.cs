using HomeTaskManager.Api.Contracts.TaskInstances;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/task-instances")]
public class TaskInstancesController(ISetTaskInstanceCompletionUseCase setTaskInstanceCompletionUseCase) : ControllerBase
{
    [HttpPut("{id:guid}/completion")]
    public async Task<ActionResult<TaskInstanceDto>> SetCompletion(
        Guid id,
        SetTaskInstanceCompletionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SetTaskInstanceCompletionCommand(id, request.RequestingUserId, request.IsCompleted);
            var instance = await setTaskInstanceCompletionUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(instance);
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
