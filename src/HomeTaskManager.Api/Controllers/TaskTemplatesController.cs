using HomeTaskManager.Api.Contracts.TaskTemplates;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeTaskManager.Api;
using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/task-templates")]
public class TaskTemplatesController(
    ICreateTaskTemplateUseCase createTaskTemplateUseCase,
    IListActiveTaskTemplatesUseCase listActiveTaskTemplatesUseCase,
    IUpdateTaskTemplateUseCase updateTaskTemplateUseCase,
    ISetTaskTemplateActiveUseCase setTaskTemplateActiveUseCase,
    ITaskTemplateRepository taskTemplateRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskTemplateDto>>> GetActiveTaskTemplates(CancellationToken cancellationToken)
    {
        var taskTemplates = await listActiveTaskTemplatesUseCase.ExecuteAsync(cancellationToken);
        return Ok(taskTemplates);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<TaskTemplateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var taskTemplates = await taskTemplateRepository.GetAllAsync(cancellationToken);
        return Ok(taskTemplates.Select(TaskTemplateMapper.ToDto).ToList());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TaskTemplateDto>> CreateTaskTemplate(CreateTaskTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateTaskTemplateCommand(
                request.Title,
                request.Description,
                request.ManualDifficultyScore,
                request.FrequencyType,
                request.Priority,
                request.EstimatedDurationMinutes,
                User.GetCurrentUserId());

            var taskTemplate = await createTaskTemplateUseCase.ExecuteAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetActiveTaskTemplates), new { }, taskTemplate);
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TaskTemplateDto>> UpdateTaskTemplate(Guid id, UpdateTaskTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateTaskTemplateCommand(
                id,
                User.GetCurrentUserId(),
                request.Title,
                request.Description,
                request.ManualDifficultyScore,
                request.Priority,
                request.FrequencyType,
                request.EstimatedDurationMinutes);

            var taskTemplate = await updateTaskTemplateUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(taskTemplate);
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

    [HttpPut("{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskTemplateDto>> SetActive(Guid id, SetTaskTemplateActiveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await setTaskTemplateActiveUseCase.ExecuteAsync(new SetTaskTemplateActiveCommand(id, User.GetCurrentUserId(), request.IsActive), cancellationToken));
        }
        catch (ForbiddenOperationException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (DomainValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
