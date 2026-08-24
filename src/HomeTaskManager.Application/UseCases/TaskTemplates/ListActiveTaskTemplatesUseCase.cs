using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Application.UseCases.TaskTemplates;

public interface IListActiveTaskTemplatesUseCase
{
    Task<IReadOnlyList<TaskTemplateDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class ListActiveTaskTemplatesUseCase(ITaskTemplateRepository taskTemplateRepository) : IListActiveTaskTemplatesUseCase
{
    public async Task<IReadOnlyList<TaskTemplateDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var taskTemplates = await taskTemplateRepository.GetActiveAsync(cancellationToken);
        return taskTemplates.Select(TaskTemplateMapper.ToDto).ToList();
    }
}
