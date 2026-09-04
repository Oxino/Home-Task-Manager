using HomeTaskManager.Api.Contracts.TaskInstances;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.TaskInstances;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;

namespace HomeTaskManager.Api.Controllers;

/// <summary>Actions on a single task instance: completion, reassignment, cancellation, comments, decision explanation, and flexible-task execution logging.</summary>
[ApiController]
[Route("api/task-instances")]
public class TaskInstancesController(ISetTaskInstanceCompletionUseCase setTaskInstanceCompletionUseCase, IReassignTaskInstanceUseCase reassignTaskInstanceUseCase, ICancelTaskInstanceUseCase cancelTaskInstanceUseCase, ITaskCommentUseCase taskCommentUseCase, IExecuteDailyFlexibleTaskUseCase executeDailyFlexibleTaskUseCase, ITaskInstanceRepository taskInstanceRepository, IWeekRepository weekRepository, IUserRepository userRepository, IAssignmentExplanationRepository assignmentExplanationRepository) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<object>> Get(Guid id, CancellationToken cancellationToken)
    {
        var instance = await taskInstanceRepository.GetByIdAsync(id, cancellationToken);
        if (instance is null) return NotFound();
        var user = await userRepository.GetByIdAsync(User.GetCurrentUserId(), cancellationToken);
        // Only the assigned participant or an administrator may view a task instance's details.
        if (user is null || (!user.IsAdmin && instance.AssignedUserId != user.Id)) return Forbid();
        var week = await weekRepository.GetByIdAsync(instance.WeekId, cancellationToken);
        if (week is null) return NotFound();
        return Ok(new { instance = WeekMapper.ToDto(instance), week = WeekMapper.ToDto(week) });
    }

    [HttpPut("{id:guid}/completion")]
    [Authorize]
    public async Task<ActionResult<TaskInstanceDto>> SetCompletion(
        Guid id,
        SetTaskInstanceCompletionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SetTaskInstanceCompletionCommand(id, User.GetCurrentUserId(), request.IsCompleted);
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

    [HttpPut("{id:guid}/assignment")]
    [Authorize]
    public async Task<ActionResult<TaskInstanceDto>> Reassign(Guid id, ReassignTaskInstanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await reassignTaskInstanceUseCase.ExecuteAsync(new ReassignTaskInstanceCommand(id, User.GetCurrentUserId(), request.AssignedUserId), cancellationToken);
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

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskInstanceDto>> Cancel(Guid id, CancelTaskInstanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await cancelTaskInstanceUseCase.ExecuteAsync(new CancelTaskInstanceCommand(id, User.GetCurrentUserId()), cancellationToken);
            return Ok(result);
        }
        catch (ForbiddenOperationException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (DomainValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{id:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<TaskCommentDto>>> GetComments(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await taskCommentUseCase.ListAsync(id, User.GetCurrentUserId(), cancellationToken)); }
        catch (ForbiddenOperationException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (DomainValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>Returns the BR-033 "why was this assigned this way" decision trace, restricted to the same viewers as <see cref="Get"/>.</summary>
    [HttpGet("{id:guid}/explanation")]
    [Authorize]
    public async Task<ActionResult<AssignmentExplanationDto>> GetExplanation(Guid id, CancellationToken cancellationToken)
    {
        var instance = await taskInstanceRepository.GetByIdAsync(id, cancellationToken);
        if (instance is null) return NotFound();
        var user = await userRepository.GetByIdAsync(User.GetCurrentUserId(), cancellationToken);
        if (user is null || (!user.IsAdmin && instance.AssignedUserId != user.Id)) return Forbid();
        var explanation = await assignmentExplanationRepository.GetByTaskInstanceIdAsync(id, cancellationToken);
        if (explanation is null) return NotFound();
        return Ok(AssignmentExplanationMapper.ToDto(explanation));
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<TaskCommentDto>> AddComment(Guid id, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await taskCommentUseCase.AddAsync(new AddTaskCommentCommand(id, User.GetCurrentUserId(), request.Content), cancellationToken)); }
        catch (ForbiddenOperationException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (DomainValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("{id:guid}/executions")]
    [Authorize]
    public async Task<ActionResult<DailyFlexibleExecutionDto>> ExecuteFlexible(Guid id, ExecuteDailyFlexibleTaskRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await executeDailyFlexibleTaskUseCase.ExecuteAsync(new ExecuteDailyFlexibleTaskCommand(id, User.GetCurrentUserId(), request.ExecutionDate), cancellationToken)); }
        catch (ForbiddenOperationException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (DomainValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
