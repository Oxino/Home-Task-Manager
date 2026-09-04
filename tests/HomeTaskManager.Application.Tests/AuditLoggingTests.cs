using FluentAssertions;
using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Audit;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Application.Tests;

public sealed record RecordedAuditEntry(string EntityType, Guid EntityId, string Action, Guid PerformedByUserId, string? OldValue, string? NewValue);

/// <summary>In-memory fake IAuditLogger that records calls for assertion instead of persisting them.</summary>
public sealed class InMemoryAuditLogger : IAuditLogger
{
    public List<RecordedAuditEntry> Entries { get; } = [];

    public Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new RecordedAuditEntry(entityType, entityId, action, performedByUserId, oldValue, newValue));
        return Task.CompletedTask;
    }
}

/// <summary>In-memory fake standing in for the EF-backed AuditLog store in tests.</summary>
public sealed class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLog> _entries = [];

    public void Seed(AuditLog entry) => _entries.Add(entry);

    public Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuditLog>>(
            _entries.Where(entry => entry.EntityType == entityType && entry.EntityId == entityId).ToList());

    public Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuditLog>>(_entries.OrderByDescending(entry => entry.PerformedAt).ToList());

    public Task<IReadOnlyList<AuditLog>> GetByPerformerAsync(Guid performerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuditLog>>(_entries.Where(entry => entry.PerformedByUserId == performerId).OrderByDescending(entry => entry.PerformedAt).ToList());

    public Task<IReadOnlyList<AuditLog>> GetByPeriodAsync(DateTime from, DateTime to, string? action = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuditLog>>(_entries.Where(entry => entry.PerformedAt >= from && entry.PerformedAt < to && (action == null || entry.Action == action)).OrderByDescending(entry => entry.PerformedAt).ToList());

    public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPageAsync(Guid? performerId, string? category, string? entityType, string? action, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = performerId.HasValue ? _entries.Where(entry => entry.PerformedByUserId == performerId.Value) : _entries;
        var ordered = query.OrderByDescending(entry => entry.PerformedAt).ToList();
        return Task.FromResult<(IReadOnlyList<AuditLog>, int)>((ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), ordered.Count));
    }
}

/// <summary>Verifies key use cases actually write an audit entry (BR-033) with the expected entity/action/actor.</summary>
public class AuditLoggingTests
{
    [Fact]
    public async Task Updating_user_preferences_records_a_user_action()
    {
        var user = new User("Julie");
        var repository = new InMemoryUserPreferencesRepository();
        var auditLogger = new InMemoryAuditLogger();
        var useCase = new UserPreferencesUseCase(repository, auditLogger, new NoOpUnitOfWork());

        await useCase.UpdateAsync(new UpdateUserPreferencesCommand(user.Id, "None", "Numeric"));

        var entry = auditLogger.Entries.Should().ContainSingle().Subject;
        entry.EntityType.Should().Be(AuditEntityTypes.UserPreferences);
        entry.Action.Should().Be(AuditActions.Updated);
        entry.PerformedByUserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Generating_instances_records_an_administration_action()
    {
        var admin = new User("Nicolas", isAdmin: true);
        var auditLogger = new InMemoryAuditLogger();
        var useCase = new GenerateWeekInstancesUseCase(
            new InMemoryWeekRepository(), new InMemoryTaskTemplateRepository(), new InMemoryTaskInstanceRepository(), new NoOpUnitOfWork(), auditLogger);

        var result = await useCase.ExecuteAsync(new GenerateWeekInstancesCommand(2026, 1, admin.Id));

        result.Week.Should().NotBeNull();
        var entry = auditLogger.Entries.Should().ContainSingle().Subject;
        entry.EntityType.Should().Be(AuditEntityTypes.Week);
        entry.Action.Should().Be(AuditActions.Generated);
        entry.PerformedByUserId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task Creating_a_user_records_an_audit_entry()
    {
        var repository = new InMemoryUserRepository();
        var auditLogger = new InMemoryAuditLogger();
        var useCase = new CreateUserUseCase(repository, auditLogger, new NoOpUnitOfWork());

        var profile = await useCase.ExecuteAsync(new CreateUserCommand("Julie", false, 100));

        var entry = auditLogger.Entries.Should().ContainSingle().Subject;
        entry.EntityType.Should().Be(AuditEntityTypes.User);
        entry.EntityId.Should().Be(profile.Id);
        entry.Action.Should().Be(AuditActions.Created);
    }

    [Fact]
    public async Task Creating_a_task_template_records_an_audit_entry_performed_by_the_author()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        userRepository.Seed(author);
        var auditLogger = new InMemoryAuditLogger();
        var useCase = new CreateTaskTemplateUseCase(new InMemoryTaskTemplateRepository(), userRepository, auditLogger, new NoOpUnitOfWork());

        var command = new CreateTaskTemplateCommand("Aspirateur", null, 40, FrequencyType.Weekly, TaskPriority.Normal, null, author.Id);
        await useCase.ExecuteAsync(command);

        var entry = auditLogger.Entries.Should().ContainSingle().Subject;
        entry.EntityType.Should().Be(AuditEntityTypes.TaskTemplate);
        entry.PerformedByUserId.Should().Be(author.Id);
        entry.NewValue.Should().Contain("Aspirateur");
    }
}

/// <summary>Verifies GetAuditLogsForEntityUseCase's various query modes (by entity, performer, period, page).</summary>
public class GetAuditLogsForEntityUseCaseTests
{
    [Fact]
    public async Task Returns_only_entries_for_the_requested_entity()
    {
        var repository = new InMemoryAuditLogRepository();
        var matchingEntityId = Guid.NewGuid();
        repository.Seed(new AuditLog("TaskTemplate", matchingEntityId, "Created", Guid.NewGuid(), null, "Title=Aspirateur"));
        repository.Seed(new AuditLog("TaskTemplate", Guid.NewGuid(), "Created", Guid.NewGuid(), null, "Title=Vaisselle"));
        var useCase = new GetAuditLogsForEntityUseCase(repository);

        var entries = await useCase.ExecuteAsync("TaskTemplate", matchingEntityId);

        entries.Should().ContainSingle().Which.EntityId.Should().Be(matchingEntityId);
    }

    [Fact]
    public async Task Returns_all_entries_in_reverse_chronological_order()
    {
        var repository = new InMemoryAuditLogRepository();
        var actor = Guid.NewGuid();
        repository.Seed(new AuditLog("TaskTemplate", Guid.NewGuid(), "Created", actor, null, "Title=Older"));
        repository.Seed(new AuditLog("User", Guid.NewGuid(), "Created", actor, null, "DisplayName=Newer"));
        var useCase = new GetAuditLogsForEntityUseCase(repository);

        var entries = await useCase.ExecuteAllAsync();

        entries.Should().HaveCount(2);
        entries[0].NewValue.Should().Be("DisplayName=Newer");
    }

    [Fact]
    public async Task Returns_only_entries_performed_by_the_requested_user()
    {
        var repository = new InMemoryAuditLogRepository();
        var actor = Guid.NewGuid();
        repository.Seed(new AuditLog("User", Guid.NewGuid(), "Updated", actor, null, "own"));
        repository.Seed(new AuditLog("User", Guid.NewGuid(), "Updated", Guid.NewGuid(), null, "other"));
        var useCase = new GetAuditLogsForEntityUseCase(repository);

        var entries = await useCase.ExecuteForPerformerAsync(actor);

        entries.Should().ContainSingle().Which.NewValue.Should().Be("own");
    }
}
