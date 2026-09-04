using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.TaskInstances;

public sealed record TaskCommentDto(Guid Id, Guid TaskInstanceId, Guid AuthorUserId, string Content, DateTime CreatedAt);
public sealed record AddTaskCommentCommand(Guid TaskInstanceId, Guid RequestingUserId, string Content);

/// <summary>Threaded discussion attached to a task instance; access restricted to admins and the assigned participant.</summary>
public interface ITaskCommentUseCase
{
    Task<IReadOnlyList<TaskCommentDto>> ListAsync(Guid taskInstanceId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<TaskCommentDto> AddAsync(AddTaskCommentCommand command, CancellationToken cancellationToken = default);
}

public sealed class TaskCommentUseCase(
    ITaskCommentRepository commentRepository,
    ITaskInstanceRepository instanceRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : ITaskCommentUseCase
{
    public async Task<IReadOnlyList<TaskCommentDto>> ListAsync(Guid taskInstanceId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(taskInstanceId, requestingUserId, cancellationToken);
        return (await commentRepository.GetByTaskInstanceAsync(taskInstanceId, cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<TaskCommentDto> AddAsync(AddTaskCommentCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(command.TaskInstanceId, command.RequestingUserId, cancellationToken);
        var comment = new TaskComment(command.TaskInstanceId, command.RequestingUserId, command.Content);
        await commentRepository.AddAsync(comment, cancellationToken);
        await auditLogger.LogAsync(AuditEntityTypes.TaskInstance, command.TaskInstanceId, AuditActions.CommentAdded, command.RequestingUserId,
            newValue: $"CommentId={comment.Id}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(comment);
    }

    private async Task EnsureAccessAsync(Guid taskInstanceId, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(requestingUserId, cancellationToken) ?? throw new DomainValidationException("The requesting profile does not exist.");
        var instance = await instanceRepository.GetByIdAsync(taskInstanceId, cancellationToken) ?? throw new DomainValidationException("The requested task instance does not exist.");
        if (!user.IsAdmin && instance.AssignedUserId != requestingUserId) throw new ForbiddenOperationException("Only the assigned participant can access this task's comments.");
    }

    private static TaskCommentDto ToDto(TaskComment comment) => new(comment.Id, comment.TaskInstanceId, comment.AuthorUserId, comment.Content, comment.CreatedAt);
}