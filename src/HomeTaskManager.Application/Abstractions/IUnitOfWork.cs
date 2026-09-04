namespace HomeTaskManager.Application.Abstractions;

/// <summary>Commits pending repository changes as a single database transaction (EF Core change tracker under the hood).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}