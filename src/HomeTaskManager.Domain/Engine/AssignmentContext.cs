using HomeTaskManager.Domain.Entities;

namespace HomeTaskManager.Domain.Engine;

/// <summary>
/// Read-only snapshot of everything an <see cref="IAssignmentStrategy"/> needs to assign a week's tasks:
/// the target week, the participants eligible to receive tasks, the instances to assign,
/// their templates (for priority/duration), each participant's recent load history (for equity),
/// and the administrable engine settings. Strategies never mutate this context or touch the database.
/// </summary>
public sealed record AssignmentContext(
    Week Week,
    IReadOnlyList<User> ActiveParticipants,
    IReadOnlyList<TaskInstance> TaskInstances,
    IReadOnlyDictionary<Guid, TaskTemplate> TaskTemplatesById,
    IReadOnlyDictionary<Guid, ParticipantHistory> HistoryByUserId,
    AssignmentEngineSettings Settings);
