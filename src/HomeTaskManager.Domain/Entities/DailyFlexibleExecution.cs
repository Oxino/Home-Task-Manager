using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Entities;

public sealed class DailyFlexibleExecution
{
    private DailyFlexibleExecution() { }

    public DailyFlexibleExecution(Guid taskInstanceId, Guid executedByUserId, DateOnly executionDate, DateOnly weekStartDate, DateOnly weekEndDate)
    {
        if (executionDate < weekStartDate || executionDate > weekEndDate) throw new DomainValidationException("The execution date must belong to the instance week.");
        Id = Guid.NewGuid();
        TaskInstanceId = taskInstanceId;
        ExecutedByUserId = executedByUserId;
        ExecutionDate = executionDate;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TaskInstanceId { get; private set; }
    public Guid ExecutedByUserId { get; private set; }
    public DateOnly ExecutionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
