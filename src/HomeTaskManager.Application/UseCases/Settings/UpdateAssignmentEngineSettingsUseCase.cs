using System.Globalization;
using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Application.UseCases.Settings;

public sealed record UpdateAssignmentEngineSettingsCommand(
    Guid RequestingUserId,
    decimal LowPriorityMultiplier,
    decimal NormalPriorityMultiplier,
    decimal CriticalPriorityMultiplier,
    int ReferenceDurationMinutes,
    int EquityWindowWeeks,
    decimal RepetitionPenaltyWeight);

public interface IUpdateAssignmentEngineSettingsUseCase
{
    Task<AssignmentEngineSettingsDto> ExecuteAsync(UpdateAssignmentEngineSettingsCommand command, CancellationToken cancellationToken = default);
}

/// <summary>BR-058: lets an administrator tune the assignment engine's parameters.</summary>
public sealed class UpdateAssignmentEngineSettingsUseCase(
    ISystemSettingRepository systemSettingRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork) : IUpdateAssignmentEngineSettingsUseCase
{
    public async Task<AssignmentEngineSettingsDto> ExecuteAsync(
        UpdateAssignmentEngineSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestingUser = await userRepository.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new DomainValidationException("The requesting profile does not exist.");

        if (!requestingUser.IsAdmin)
        {
            throw new ForbiddenOperationException("Only an administrator can change the assignment engine settings.");
        }

        if (command.LowPriorityMultiplier <= 0 || command.NormalPriorityMultiplier <= 0 || command.CriticalPriorityMultiplier <= 0)
        {
            throw new DomainValidationException("Priority multipliers must be strictly positive.");
        }

        if (command.ReferenceDurationMinutes < 1)
        {
            throw new DomainValidationException("The reference duration must be at least one minute.");
        }

        if (command.EquityWindowWeeks < 1)
        {
            throw new DomainValidationException("The equity window must span at least one week.");
        }

        if (command.RepetitionPenaltyWeight < 0)
        {
            throw new DomainValidationException("The repetition penalty weight cannot be negative.");
        }

        await UpsertAsync(AssignmentEngineSettingKeys.LowPriorityMultiplier, command.LowPriorityMultiplier, requestingUser.Id, cancellationToken);
        await UpsertAsync(AssignmentEngineSettingKeys.NormalPriorityMultiplier, command.NormalPriorityMultiplier, requestingUser.Id, cancellationToken);
        await UpsertAsync(AssignmentEngineSettingKeys.CriticalPriorityMultiplier, command.CriticalPriorityMultiplier, requestingUser.Id, cancellationToken);
        await UpsertAsync(AssignmentEngineSettingKeys.ReferenceDurationMinutes, command.ReferenceDurationMinutes, requestingUser.Id, cancellationToken);
        await UpsertAsync(AssignmentEngineSettingKeys.EquityWindowWeeks, command.EquityWindowWeeks, requestingUser.Id, cancellationToken);
        await UpsertAsync(AssignmentEngineSettingKeys.RepetitionPenaltyWeight, command.RepetitionPenaltyWeight, requestingUser.Id, cancellationToken);

        // No single aggregate entity backs these settings; Guid.Empty identifies the singleton configuration.
        await auditLogger.LogAsync(
            AuditEntityTypes.AssignmentEngineSettings, Guid.Empty, AuditActions.Updated, requestingUser.Id,
            newValue: $"EquityWindowWeeks={command.EquityWindowWeeks}; RepetitionPenaltyWeight={command.RepetitionPenaltyWeight}",
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AssignmentEngineSettingsDto(
            command.LowPriorityMultiplier,
            command.NormalPriorityMultiplier,
            command.CriticalPriorityMultiplier,
            command.ReferenceDurationMinutes,
            command.EquityWindowWeeks,
            command.RepetitionPenaltyWeight);
    }

    private async Task UpsertAsync(string key, decimal value, Guid updatedByUserId, CancellationToken cancellationToken) =>
        await UpsertAsync(key, value.ToString(CultureInfo.InvariantCulture), updatedByUserId, cancellationToken);

    private async Task UpsertAsync(string key, int value, Guid updatedByUserId, CancellationToken cancellationToken) =>
        await UpsertAsync(key, value.ToString(CultureInfo.InvariantCulture), updatedByUserId, cancellationToken);

    private async Task UpsertAsync(string key, string value, Guid updatedByUserId, CancellationToken cancellationToken)
    {
        var existing = await systemSettingRepository.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            await systemSettingRepository.AddAsync(new SystemSetting(key, value, updatedByUserId), cancellationToken);
            return;
        }

        existing.SetValue(value, updatedByUserId);
    }
}
