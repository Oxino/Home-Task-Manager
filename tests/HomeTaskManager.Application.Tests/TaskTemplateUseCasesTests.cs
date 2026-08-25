using FluentAssertions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.Tests;

public sealed class InMemoryTaskTemplateRepository : ITaskTemplateRepository
{
    private readonly List<TaskTemplate> _taskTemplates = [];

    public void Seed(TaskTemplate taskTemplate) => _taskTemplates.Add(taskTemplate);

    public Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_taskTemplates.SingleOrDefault(task => task.Id == id));

    public Task<IReadOnlyList<TaskTemplate>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskTemplate>>(_taskTemplates.Where(task => task.IsActive).ToList());

    public Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskTemplate>>(_taskTemplates.ToList());

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default) =>
        Task.FromResult(_taskTemplates.Any(task => string.Equals(task.Title, title, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByTitleAsync(string title, Guid excludingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_taskTemplates.Any(task => task.Id != excludingId && string.Equals(task.Title, title, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<TaskTemplate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskTemplate>>(_taskTemplates.Where(task => ids.Contains(task.Id)).ToList());

    public Task AddAsync(TaskTemplate taskTemplate, CancellationToken cancellationToken = default)
    {
        _taskTemplates.Add(taskTemplate);
        return Task.CompletedTask;
    }
}

public class CreateTaskTemplateUseCaseTests
{
    [Fact]
    public async Task Creates_a_task_template_for_an_active_author()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        userRepository.Seed(author);
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        var useCase = new CreateTaskTemplateUseCase(taskTemplateRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var command = new CreateTaskTemplateCommand("Aspirateur", "Salon", 40, FrequencyType.Weekly, TaskPriority.Normal, 30, author.Id);
        var taskTemplate = await useCase.ExecuteAsync(command);

        taskTemplate.Title.Should().Be("Aspirateur");
        taskTemplate.CreatedByUserId.Should().Be(author.Id);
    }

    [Fact]
    public async Task Rejects_a_duplicate_title()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        userRepository.Seed(author);
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly));
        var useCase = new CreateTaskTemplateUseCase(taskTemplateRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var command = new CreateTaskTemplateCommand("Aspirateur", null, 40, FrequencyType.Weekly, TaskPriority.Normal, null, author.Id);
        var action = () => useCase.ExecuteAsync(command);

        await action.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Rejects_an_inactive_author()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        author.SetActive(false);
        userRepository.Seed(author);
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        var useCase = new CreateTaskTemplateUseCase(taskTemplateRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var command = new CreateTaskTemplateCommand("Aspirateur", null, 40, FrequencyType.Weekly, TaskPriority.Normal, null, author.Id);
        var action = () => useCase.ExecuteAsync(command);

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}

public class UpdateTaskTemplateUseCaseTests
{
    [Fact]
    public async Task Author_can_modify_their_own_task()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        userRepository.Seed(author);
        var taskTemplate = new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly);
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(taskTemplate);
        var useCase = new UpdateTaskTemplateUseCase(taskTemplateRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var command = new UpdateTaskTemplateCommand(
            taskTemplate.Id, author.Id, "Aspirateur salon", "Description", 50, TaskPriority.Critical, FrequencyType.Weekly, 45);
        var updated = await useCase.ExecuteAsync(command);

        updated.Title.Should().Be("Aspirateur salon");
        updated.Priority.Should().Be(nameof(TaskPriority.Critical));
    }

    [Fact]
    public async Task Another_user_cannot_modify_someone_elses_task()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        var otherUser = new User("Nicolas");
        userRepository.Seed(author);
        userRepository.Seed(otherUser);
        var taskTemplate = new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly);
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(taskTemplate);
        var useCase = new UpdateTaskTemplateUseCase(taskTemplateRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var command = new UpdateTaskTemplateCommand(
            taskTemplate.Id, otherUser.Id, "Aspirateur salon", null, 40, TaskPriority.Normal, FrequencyType.Weekly, null);
        var action = () => useCase.ExecuteAsync(command);

        await action.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task Admin_can_modify_any_task()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        var admin = new User("Nicolas", isAdmin: true);
        userRepository.Seed(author);
        userRepository.Seed(admin);
        var taskTemplate = new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly);
        var taskTemplateRepository = new InMemoryTaskTemplateRepository();
        taskTemplateRepository.Seed(taskTemplate);
        var useCase = new UpdateTaskTemplateUseCase(taskTemplateRepository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var command = new UpdateTaskTemplateCommand(
            taskTemplate.Id, admin.Id, "Aspirateur salon", null, 40, TaskPriority.Normal, FrequencyType.Weekly, null);
        var updated = await useCase.ExecuteAsync(command);

        updated.Title.Should().Be("Aspirateur salon");
    }

    [Fact]
    public async Task Cannot_rename_a_task_to_an_existing_title()
    {
        var userRepository = new InMemoryUserRepository();
        var author = new User("Julie");
        userRepository.Seed(author);
        var firstTask = new TaskTemplate("Aspirateur", author.Id, 40, FrequencyType.Weekly);
        var secondTask = new TaskTemplate("Vaisselle", author.Id, 30, FrequencyType.Weekly);
        var repository = new InMemoryTaskTemplateRepository();
        repository.Seed(firstTask);
        repository.Seed(secondTask);
        var useCase = new UpdateTaskTemplateUseCase(repository, userRepository, new NoOpAuditLogger(), new NoOpUnitOfWork());

        var action = () => useCase.ExecuteAsync(new UpdateTaskTemplateCommand(secondTask.Id, author.Id, "Aspirateur", null, 30, TaskPriority.Normal, FrequencyType.Weekly, null));

        await action.Should().ThrowAsync<DomainValidationException>();
    }
}
