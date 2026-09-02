using System.ComponentModel.DataAnnotations;

namespace HomeTaskManager.Api.Contracts.TaskInstances;

public sealed record AddTaskCommentRequest([Required, StringLength(4000, MinimumLength = 1)] string Content);
