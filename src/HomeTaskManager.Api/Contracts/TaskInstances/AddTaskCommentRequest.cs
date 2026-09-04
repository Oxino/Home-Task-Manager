using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.TaskInstances;

/// <summary>POST /api/task-instances/{id}/comments body.</summary>
public sealed record AddTaskCommentRequest([Required, StringLength(4000, MinimumLength = 1)] string Content);
