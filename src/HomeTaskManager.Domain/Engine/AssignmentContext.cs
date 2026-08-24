using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

public sealed record AssignmentContext(
    Week Week,
    IReadOnlyList<User> ActiveParticipants,
    IReadOnlyList<TaskInstance> TaskInstances,
    IReadOnlyDictionary<Guid, TaskTemplate> TaskTemplatesById,
    IReadOnlyDictionary<Guid, ParticipantHistory> HistoryByUserId,
    AssignmentEngineSettings Settings);
