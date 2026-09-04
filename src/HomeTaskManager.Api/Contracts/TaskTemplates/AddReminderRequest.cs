using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Contracts.TaskTemplates;

/// <summary>POST /api/task-templates/{id}/reminders body.</summary>
public sealed record AddReminderRequest(ReminderTriggerType TriggerType, int OffsetValue, ReminderOffsetUnit OffsetUnit);
