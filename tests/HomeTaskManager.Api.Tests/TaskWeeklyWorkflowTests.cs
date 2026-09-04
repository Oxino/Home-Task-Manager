using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeTaskManager.Api.Contracts.Assignments;
using HomeTaskManager.Api.Contracts.TaskInstances;
using HomeTaskManager.Api.Contracts.TaskTemplates;
using HomeTaskManager.Api.Contracts.Users;
using HomeTaskManager.Api.Contracts.Weeks;
using HomeTaskManager.Application.UseCases.Assignments;
using HomeTaskManager.Application.UseCases.TaskTemplates;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Application.UseCases.Weeks;
using HomeTaskManager.Domain.Enums;

namespace HomeTaskManager.Api.Tests;

/// <summary>End-to-end HTTP tests of the full weekly workflow: generate instances, run the assignment engine, complete/reassign tasks, and close the week.</summary>
public sealed class TaskWeeklyWorkflowTests : IDisposable
{
    private readonly HomeTaskManagerApiFactory _factory = new();
    private readonly HttpClient _client;

    public TaskWeeklyWorkflowTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FullWeeklyWorkflow_CreateTemplate_Generate_Assign_Complete_Close()
    {
        var today = DateTime.UtcNow;
        var year = ISOWeek.GetYear(today);
        var weekNumber = ISOWeek.GetWeekOfYear(today);

        // Bootstrap admin, then a participant.
        var admin = await CreateAndSelectProfileAsync($"Admin-{Guid.NewGuid():N}", isAdmin: true);
        var participant = await CreateProfileAsync($"Participant-{Guid.NewGuid():N}", isAdmin: false);

        // Admin creates a weekly task template.
        var createTemplateResponse = await _client.PostAsJsonAsync("/api/task-templates", new CreateTaskTemplateRequest(
            Title: $"Nettoyage-{Guid.NewGuid():N}",
            Description: "Tâche de test d'intégration",
            ManualDifficultyScore: 40,
            FrequencyType: FrequencyType.Weekly,
            Priority: TaskPriority.Normal,
            EstimatedDurationMinutes: 30,
            CreatedByUserId: admin.Id));
        createTemplateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Admin generates instances for the current week.
        var generateResponse = await _client.PostAsJsonAsync("/api/weeks/generate", new GenerateWeekInstancesRequest(year, weekNumber));
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var generated = await generateResponse.Content.ReadFromJsonAsync<GenerateWeekInstancesResult>(TestJson.Options);
        generated!.CreatedInstances.Should().NotBeEmpty();

        // Admin runs the assignment engine for that week.
        var runResponse = await _client.PostAsJsonAsync("/api/assignments/run", new RunAssignmentEngineRequest(year, weekNumber, admin.Id));
        runResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var runResult = await runResponse.Content.ReadFromJsonAsync<RunAssignmentEngineResult>(TestJson.Options);
        var assignedInstance = runResult!.Instances.Single(instance => instance.AssignedUserId is not null);
        new[] { admin.Id, participant.Id }.Should().Contain(assignedInstance.AssignedUserId!.Value);

        // Whichever participant the engine assigned marks their instance complete.
        var assignedUserId = assignedInstance.AssignedUserId!.Value;
        await SelectProfileAsync(assignedUserId);
        var completionResponse = await _client.PutAsJsonAsync(
            $"/api/task-instances/{assignedInstance.Id}/completion",
            new SetTaskInstanceCompletionRequest(assignedUserId, true));
        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Admin closes the week.
        await SelectProfileAsync(admin.Id);
        var closeResponse = await _client.PostAsJsonAsync($"/api/weeks/{year}/{weekNumber}/close", new CloseWeekRequest());
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closeResult = await closeResponse.Content.ReadFromJsonAsync<CloseWeekResult>(TestJson.Options);
        closeResult!.Week.Status.Should().Be("Closed");
    }

    private async Task<UserProfileDto> CreateProfileAsync(string displayName, bool isAdmin)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest(displayName, isAdmin, 100));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserProfileDto>(TestJson.Options))!;
    }

    private async Task<UserProfileDto> CreateAndSelectProfileAsync(string displayName, bool isAdmin)
    {
        var profile = await CreateProfileAsync(displayName, isAdmin);
        await SelectProfileAsync(profile.Id);
        return profile;
    }

    private async Task SelectProfileAsync(Guid userId)
    {
        var response = await _client.PostAsync($"/api/users/{userId}/select", content: null);
        response.EnsureSuccessStatusCode();
    }
}
