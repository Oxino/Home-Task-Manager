using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Contracts.TaskTemplates;

public sealed record AddReminderRequest(ReminderTriggerType TriggerType, int OffsetValue, ReminderOffsetUnit OffsetUnit);
