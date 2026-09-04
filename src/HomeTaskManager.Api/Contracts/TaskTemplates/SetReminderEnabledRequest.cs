namespace HomeTaskManager.Api.Contracts.TaskTemplates;

/// <summary>PUT /api/task-templates/{id}/reminders/{reminderId}/active body.</summary>
public sealed record SetReminderEnabledRequest(bool IsEnabled);