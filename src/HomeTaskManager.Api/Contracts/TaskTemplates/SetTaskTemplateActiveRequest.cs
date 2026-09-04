namespace HomeTaskManager.Api.Contracts.TaskTemplates;

/// <summary>PUT /api/task-templates/{id}/active body.</summary>
public sealed record SetTaskTemplateActiveRequest(bool IsActive);