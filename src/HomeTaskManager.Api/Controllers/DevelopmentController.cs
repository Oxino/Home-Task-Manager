using HomeTaskManager.Api.Contracts.Development;
using HomeTaskManager.Api;
using HomeTaskManager.Application.Abstractions;
using HomeTaskManager.Application.Abstractions.Repositories;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Enums;
using HomeTaskManager.Domain.ValueObjects;
using HomeTaskManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/development")]
[Authorize(Roles = "Admin")]
[DevelopmentOnly]
public sealed class DevelopmentController(
    HomeTaskManagerDbContext dbContext,
    IGenerateWeekInstancesUseCase generateWeekInstancesUseCase,
    IRunAssignmentEngineUseCase runAssignmentEngineUseCase,
    IWeekRepository weekRepository,
    IAssignmentRunRepository assignmentRunRepository) : ControllerBase
{
    [HttpPost("seed-history")]
    public async Task<ActionResult<object>> SeedHistory(SeedHistoryRequest request, CancellationToken cancellationToken)
    {
        if (request.Weeks is < 1 or > 52)
            return BadRequest(new { error = "Weeks must be between 1 and 52." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentWeek = ISOWeek.GetWeekOfYear(today.ToDateTime(TimeOnly.MinValue));
        var currentYear = ISOWeek.GetYear(today.ToDateTime(TimeOnly.MinValue));
        var generatedWeeks = new List<object>();

        for (var offset = request.Weeks - 1; offset >= 0; offset--)
        {
            var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(currentYear, currentWeek, DayOfWeek.Monday)).AddDays(-offset * 7);
            var year = ISOWeek.GetYear(monday.ToDateTime(TimeOnly.MinValue));
            var weekNumber = ISOWeek.GetWeekOfYear(monday.ToDateTime(TimeOnly.MinValue));
            var generated = await generateWeekInstancesUseCase.ExecuteAsync(new GenerateWeekInstancesCommand(year, weekNumber), cancellationToken);
            var week = await weekRepository.GetByKeyAsync(new Domain.ValueObjects.WeekKey(year, weekNumber), cancellationToken);
            var alreadyExecuted = week is not null && await assignmentRunRepository.ExistsForWeekAsync(week.Id, cancellationToken);
            if (!alreadyExecuted)
                await runAssignmentEngineUseCase.ExecuteAsync(new RunAssignmentEngineCommand(year, weekNumber, User.GetCurrentUserId()), cancellationToken);
            generatedWeeks.Add(new { year, weekNumber, createdInstances = generated.CreatedInstances.Count, executionCreated = !alreadyExecuted });
        }

        return Ok(new { weeks = generatedWeeks });
    }

    [HttpPost("reset-and-seed-full-demo")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ResetAndSeedFullDemo(FullDemoSeedRequest request, CancellationToken cancellationToken)
    {
//TODO: expliquer cette partie de code. PK restreindre les semaines entre 6 et 20?
        if (request.Weeks is < 6 or > 20)
            return BadRequest(new { error = "Weeks must be between 6 and 20." });

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);

        var admin1 = new User("admin1", isAdmin: true, availabilityPercentage: 90);
        var user1 = new User("user1", isAdmin: false, availabilityPercentage: 100);
        dbContext.Users.AddRange(admin1, user1);
        dbContext.UserPreferences.AddRange(new UserPreferences(admin1.Id), new UserPreferences(user1.Id));

        var templates = BuildTemplates(admin1.Id, user1.Id);
        dbContext.TaskTemplates.AddRange(templates);
        foreach (var template in templates)
        {
            var trigger = template.FrequencyType == FrequencyType.DailyMandatory || template.FrequencyType == FrequencyType.DailyFlexible
                ? ReminderTriggerType.EndOfDay
                : ReminderTriggerType.EndOfWeek;
            var unit = trigger == ReminderTriggerType.EndOfDay ? ReminderOffsetUnit.Hour : ReminderOffsetUnit.Day;
            var offset = trigger == ReminderTriggerType.EndOfDay ? 4 : 1;
            dbContext.Reminders.Add(new Reminder(template.Id, trigger, offset, unit));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(ISOWeek.GetYear(DateTime.UtcNow), ISOWeek.GetWeekOfYear(DateTime.UtcNow), DayOfWeek.Monday));
        var startMonday = monday.AddDays(-7 * (request.Weeks - 1));
        var generatedWeeks = new List<(int Year, int WeekNumber, Guid WeekId)>();

        for (var i = 0; i < request.Weeks; i++)
        {
            var target = startMonday.AddDays(i * 7);
            var year = ISOWeek.GetYear(target.ToDateTime(TimeOnly.MinValue));
            var weekNumber = ISOWeek.GetWeekOfYear(target.ToDateTime(TimeOnly.MinValue));

            var generated = await generateWeekInstancesUseCase.ExecuteAsync(new GenerateWeekInstancesCommand(year, weekNumber), cancellationToken);
            await runAssignmentEngineUseCase.ExecuteAsync(new RunAssignmentEngineCommand(year, weekNumber, admin1.Id), cancellationToken);
            generatedWeeks.Add((year, weekNumber, generated.Week.Id));
        }

        var scenarios = new[]
        {
            "Normal", "AdminOnly", "UserOnly", "LowCompletion", "Alternating", "AdminMostly", "UserMostly", "CarryOverHeavy", "Normal", "ActiveMixed"
        };

        var applied = new List<object>();

        for (var i = 0; i < generatedWeeks.Count; i++)
        {
            var weekInfo = generatedWeeks[i];
            var scenario = scenarios[Math.Min(i, scenarios.Length - 1)];

            var week = await dbContext.Weeks.SingleAsync(entry => entry.Id == weekInfo.WeekId, cancellationToken);
            var instances = await dbContext.TaskInstances
                .Where(entry => entry.WeekId == week.Id && entry.AssignedUserId.HasValue && entry.Status != TaskInstanceStatus.Cancelled)
                .OrderBy(entry => entry.PlannedDate)
                .ThenBy(entry => entry.Id)
                .ToListAsync(cancellationToken);

            ApplyScenarioCompletions(instances, week, scenario, admin1.Id, user1.Id);
            AddCompletionAudits(instances, week, admin1.Id, user1.Id);
            await dbContext.SaveChangesAsync(cancellationToken);

            await AddCommentAndReactionSamplesAsync(instances, admin1.Id, user1.Id, cancellationToken);
            await AddDailyFlexibleExecutionsAsync(instances, week, templates, admin1.Id, user1.Id, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var shouldCloseWeek = i < generatedWeeks.Count - 1;
            if (shouldCloseWeek)
            {
                week.Close();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            applied.Add(new
            {
                weekInfo.Year,
                weekInfo.WeekNumber,
                scenario,
                closed = shouldCloseWeek,
                assignedInstances = instances.Count,
                completedInstances = instances.Count(entry => entry.IsCompleted)
            });
        }

        await EnsureAdminCarryOverScenarioAsync(generatedWeeks, admin1.Id, cancellationToken);

        // Add a cancellation sample on the latest (still active) week.
        var latestWeekId = generatedWeeks[^1].WeekId;
        var cancellable = await dbContext.TaskInstances
            .Where(entry => entry.WeekId == latestWeekId && !entry.IsCompleted && entry.Status == TaskInstanceStatus.Pending)
            .OrderBy(entry => entry.PlannedDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (cancellable is not null)
        {
            cancellable.Cancel();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            users = new[]
            {
                new { admin1.Id, admin1.DisplayName, admin1.IsAdmin },
                new { user1.Id, user1.DisplayName, user1.IsAdmin }
            },
            templates = templates.Count,
            reminders = templates.Count,
            weeks = applied
        });
    }

    [HttpPost("ensure-admin-carry-over")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> EnsureAdminCarryOver(CancellationToken cancellationToken)
    {
        var admin = await dbContext.Users.FirstOrDefaultAsync(entry => entry.DisplayName == "admin1" && entry.IsAdmin, cancellationToken);
        if (admin is null) return NotFound(new { error = "admin1 not found." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentKey = new WeekKey(ISOWeek.GetYear(today.ToDateTime(TimeOnly.MinValue)), ISOWeek.GetWeekOfYear(today.ToDateTime(TimeOnly.MinValue)));
        var currentWeek = await dbContext.Weeks.SingleOrDefaultAsync(entry => entry.Year == currentKey.Year && entry.WeekNumber == currentKey.WeekNumber, cancellationToken);
        if (currentWeek is null) return BadRequest(new { error = "The current week has not been generated yet." });

        var previousKey = WeekKey.FromDate(currentKey.StartDate.AddDays(-7));
        var previousWeek = await dbContext.Weeks.SingleOrDefaultAsync(entry => entry.Year == previousKey.Year && entry.WeekNumber == previousKey.WeekNumber, cancellationToken);
        if (previousWeek is null) return BadRequest(new { error = "The previous week has not been generated yet." });

        var result = await EnsureAdminCarryOverScenarioAsync(
            [(previousWeek.Year, previousWeek.WeekNumber, previousWeek.Id), (currentWeek.Year, currentWeek.WeekNumber, currentWeek.Id)],
            admin.Id,
            cancellationToken);
        return Ok(result);
    }

    private async Task<object> EnsureAdminCarryOverScenarioAsync(
        IReadOnlyList<(int Year, int WeekNumber, Guid WeekId)> generatedWeeks,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        if (generatedWeeks.Count < 2) return new { created = false, reason = "At least two weeks are required." };

        var previous = generatedWeeks[^2];
        var current = generatedWeeks[^1];
        var template = await dbContext.TaskTemplates.FirstOrDefaultAsync(entry => entry.Title == "Tâche reportée admin1", cancellationToken);
        if (template is null)
        {
            template = new TaskTemplate("Tâche reportée admin1", adminId, 30, FrequencyType.Weekly, TaskPriority.Normal, "Scénario de report pour admin1", 30);
            dbContext.TaskTemplates.Add(template);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var previousInstance = await dbContext.TaskInstances.FirstOrDefaultAsync(entry => entry.WeekId == previous.WeekId && entry.TaskTemplateId == template.Id, cancellationToken);
        if (previousInstance is null)
        {
            var previousWeek = await dbContext.Weeks.SingleAsync(entry => entry.Id == previous.WeekId, cancellationToken);
            previousInstance = new TaskInstance(template.Id, null, previousWeek, adminId, previousWeek.StartDate, previousWeek.EndDate);
            dbContext.TaskInstances.Add(previousInstance);
        }

        var carryOver = await dbContext.TaskInstances.FirstOrDefaultAsync(entry => entry.WeekId == current.WeekId && entry.TaskTemplateId == template.Id && entry.IsCarryOver, cancellationToken);
        if (carryOver is null)
        {
            var currentWeek = await dbContext.Weeks.SingleAsync(entry => entry.Id == current.WeekId, cancellationToken);
            var plannedDate = currentWeek.StartDate;
            while (await dbContext.TaskInstances.AnyAsync(entry => entry.WeekId == currentWeek.Id && entry.TaskTemplateId == template.Id && entry.PlannedDate == plannedDate, cancellationToken))
                plannedDate = plannedDate.AddDays(1);
            carryOver = new TaskInstance(template.Id, null, currentWeek, adminId, plannedDate, plannedDate, true, previousInstance.Id);
            dbContext.TaskInstances.Add(carryOver);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new { created = true, templateId = template.Id, previousInstanceId = previousInstance.Id, carryOverInstanceId = carryOver.Id, adminId };
    }

    [HttpPost("backfill-notification-instance-links")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> BackfillNotificationInstanceLinks(CancellationToken cancellationToken)
    {
        var notifications = await dbContext.Notifications
            .Where(entry => entry.Link != null && entry.Link.StartsWith("/week?") && !entry.Link.Contains("instanceId="))
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);

        var templatesByTitle = await dbContext.TaskTemplates
            .ToDictionaryAsync(entry => entry.Title, entry => entry.Id, cancellationToken);

        var weeksByKey = await dbContext.Weeks
            .ToDictionaryAsync(entry => $"{entry.Year}-{entry.WeekNumber}", entry => entry.Id, cancellationToken);

        var updated = 0;
        var skipped = 0;

        foreach (var notification in notifications)
        {
            if (!TryParseYearWeek(notification.Link!, out var year, out var weekNumber))
            {
                skipped++;
                continue;
            }

            var marker = " vous a été affectée";
            var markerIndex = notification.Message.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex <= 0)
            {
                skipped++;
                continue;
            }

            var taskTitle = notification.Message[..markerIndex].Trim();
            if (!templatesByTitle.TryGetValue(taskTitle, out var taskTemplateId))
            {
                skipped++;
                continue;
            }

            if (!weeksByKey.TryGetValue($"{year}-{weekNumber}", out var weekId))
            {
                skipped++;
                continue;
            }

            var instanceId = await dbContext.TaskInstances
                .Where(entry => entry.WeekId == weekId && entry.TaskTemplateId == taskTemplateId && entry.AssignedUserId == notification.UserId)
                .OrderBy(entry => entry.PlannedDate)
                .Select(entry => (Guid?)entry.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!instanceId.HasValue)
            {
                skipped++;
                continue;
            }

            notification.SetLink($"/week?year={year}&week={weekNumber}&instanceId={instanceId.Value}");
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { processed = notifications.Count, updated, skipped });
    }

    private static List<TaskTemplate> BuildTemplates(Guid adminId, Guid userId)
    {
        var specs = new (string Title, FrequencyType Frequency, TaskPriority Priority, int Score, int Duration, Guid Owner)[]
        {
            ("Vaisselle quotidienne", FrequencyType.DailyMandatory, TaskPriority.Normal, 25, 20, adminId),
            ("Litière chat", FrequencyType.DailyMandatory, TaskPriority.Critical, 35, 10, userId),
            ("Aérer les pièces", FrequencyType.DailyMandatory, TaskPriority.Low, 10, 5, adminId),
            ("Sortir les poubelles", FrequencyType.DailyMandatory, TaskPriority.Normal, 20, 8, userId),
            ("Arroser les plantes", FrequencyType.DailyFlexible, TaskPriority.Low, 15, 12, adminId),
            ("Vérifier courrier", FrequencyType.DailyFlexible, TaskPriority.Normal, 18, 6, userId),
            ("Rangement rapide", FrequencyType.DailyFlexible, TaskPriority.Normal, 22, 15, adminId),
            ("Nettoyage cuisine", FrequencyType.DailyFlexible, TaskPriority.Critical, 40, 30, userId),
            ("Aspirateur étage", FrequencyType.Weekly, TaskPriority.Normal, 45, 45, adminId),
            ("Nettoyage salle de bain", FrequencyType.Weekly, TaskPriority.Critical, 55, 40, userId),
            ("Courses hebdo", FrequencyType.Weekly, TaskPriority.Critical, 60, 60, adminId),
            ("Lessive", FrequencyType.Weekly, TaskPriority.Normal, 35, 50, userId),
            ("Repassage", FrequencyType.Weekly, TaskPriority.Low, 28, 35, adminId),
            ("Dépoussiérage", FrequencyType.Weekly, TaskPriority.Low, 20, 25, userId),
            ("Nettoyage vitres", FrequencyType.Monthly, TaskPriority.Normal, 65, 75, adminId),
            ("Tri administratif", FrequencyType.Monthly, TaskPriority.Normal, 40, 50, userId),
            ("Révision frigo", FrequencyType.Monthly, TaskPriority.Critical, 70, 55, adminId),
            ("Inventaire produits", FrequencyType.Monthly, TaskPriority.Low, 25, 25, userId),
            ("Monter meuble", FrequencyType.OneTime, TaskPriority.Critical, 85, 120, adminId),
            ("Nettoyage cave", FrequencyType.OneTime, TaskPriority.Normal, 72, 95, userId),
        };

        return specs
            .Select(entry => new TaskTemplate(
                entry.Title,
                entry.Owner,
                entry.Score,
                entry.Frequency,
                entry.Priority,
                description: $"Tâche seedée: {entry.Title}",
                estimatedDurationMinutes: entry.Duration))
            .ToList();
    }

    private static void ApplyScenarioCompletions(
        IReadOnlyList<TaskInstance> instances,
        Week week,
        string scenario,
        Guid adminId,
        Guid userId)
    {
        foreach (var instance in instances)
        {
            var assignedUserId = instance.AssignedUserId!.Value;
            var key = Math.Abs(HashCode.Combine(instance.Id, week.WeekNumber));
            var complete = scenario switch
            {
                "AdminOnly" => assignedUserId == adminId,
                "UserOnly" => assignedUserId == userId,
                "LowCompletion" => key % 10 < 2,
                "Alternating" => instance.PlannedDate.DayNumber % 2 == 0,
                "AdminMostly" => assignedUserId == adminId || key % 10 < 3,
                "UserMostly" => assignedUserId == userId || key % 10 < 3,
                "CarryOverHeavy" => key % 10 < 2,
                "ActiveMixed" => key % 10 < 5,
                _ => key % 10 < 7
            };

            var completionAt = week.StartDate.AddDays(Math.Abs(key % 7)).ToDateTime(new TimeOnly(18, 0));
            instance.SetCompletionAt(complete, week, completionAt.ToUniversalTime());
        }
    }

    private void AddCompletionAudits(IReadOnlyList<TaskInstance> instances, Week week, Guid adminId, Guid userId)
    {
        foreach (var instance in instances)
        {
            if (!instance.IsCompleted || instance.CompletedAt is not DateTime completedAt) continue;
            var performedBy = instance.AssignedUserId == userId ? userId : adminId;
            dbContext.AuditLogs.Add(new AuditLog(
                AuditEntityTypes.TaskInstance,
                instance.Id,
                AuditActions.CompletionChanged,
                performedBy,
                "IsCompleted=False",
                "IsCompleted=True",
                completedAt));
        }
    }

    [HttpPost("backfill-completion-audits")]
    public async Task<ActionResult<object>> BackfillCompletionAudits(CancellationToken cancellationToken)
    {
        var completedInstances = await dbContext.TaskInstances
            .Where(entry => entry.IsCompleted && entry.AssignedUserId.HasValue)
            .ToListAsync(cancellationToken);
        var instanceIds = completedInstances.Select(entry => entry.Id).ToList();
        var existingIds = await dbContext.AuditLogs
            .Where(entry => entry.EntityType == AuditEntityTypes.TaskInstance && entry.Action == AuditActions.CompletionChanged && instanceIds.Contains(entry.EntityId))
            .Select(entry => entry.EntityId)
            .ToHashSetAsync(cancellationToken);
        var weeks = await dbContext.Weeks.ToDictionaryAsync(entry => entry.Id, cancellationToken);
        var created = 0;

        foreach (var instance in completedInstances.Where(entry => !existingIds.Contains(entry.Id)))
        {
            if (!weeks.TryGetValue(instance.WeekId, out var week)) continue;
            var completionAt = instance.CompletedAt ?? week.EndDate.ToDateTime(new TimeOnly(18, 0)).ToUniversalTime();
            instance.RestoreCompletionAt(completionAt);
            dbContext.AuditLogs.Add(new AuditLog(AuditEntityTypes.TaskInstance, instance.Id, AuditActions.CompletionChanged, instance.AssignedUserId!.Value, "IsCompleted=False", "IsCompleted=True", completionAt));
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { completedInstances = completedInstances.Count, auditsCreated = created });
    }

    private async Task AddCommentAndReactionSamplesAsync(
        IReadOnlyList<TaskInstance> instances,
        Guid adminId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        foreach (var instance in instances.Take(2))
        {
            var parent = new TaskComment(instance.Id, adminId, "Point hebdo: tâche suivie et validée.");
            var reply = new TaskComment(instance.Id, userId, "Bien noté, je continue sur ce rythme.", parent.Id);
            dbContext.TaskComments.AddRange(parent, reply);
            dbContext.Reactions.AddRange(
                new Reaction(parent.Id, userId, "👍"),
                new Reaction(reply.Id, adminId, "✅"));
        }

        await Task.CompletedTask;
    }

    private async Task AddDailyFlexibleExecutionsAsync(
        IReadOnlyList<TaskInstance> instances,
        Week week,
        IReadOnlyList<TaskTemplate> templates,
        Guid adminId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var templateById = templates.ToDictionary(entry => entry.Id);
        var flexibleInstances = instances
            .Where(entry => templateById.TryGetValue(entry.TaskTemplateId, out var template) && template.FrequencyType == FrequencyType.DailyFlexible)
            .Take(4)
            .ToList();

        foreach (var instance in flexibleInstances)
        {
            var assigned = instance.AssignedUserId ?? adminId;
            var firstDay = week.StartDate;
            dbContext.DailyFlexibleExecutions.Add(new DailyFlexibleExecution(instance.Id, assigned, firstDay, week.StartDate, week.EndDate));
            if (firstDay.AddDays(2) <= week.EndDate)
                dbContext.DailyFlexibleExecutions.Add(new DailyFlexibleExecution(instance.Id, assigned, firstDay.AddDays(2), week.StartDate, week.EndDate));
        }

        await Task.CompletedTask;
    }

    private static bool TryParseYearWeek(string link, out int year, out int weekNumber)
    {
        year = 0;
        weekNumber = 0;

        var queryIndex = link.IndexOf('?');
        if (queryIndex < 0 || queryIndex + 1 >= link.Length) return false;

        var query = link[(queryIndex + 1)..];
        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0] == "year" && int.TryParse(kv[1], out var parsedYear)) year = parsedYear;
            if (kv[0] == "week" && int.TryParse(kv[1], out var parsedWeek)) weekNumber = parsedWeek;
        }

        return year > 0 && weekNumber > 0;
    }
}