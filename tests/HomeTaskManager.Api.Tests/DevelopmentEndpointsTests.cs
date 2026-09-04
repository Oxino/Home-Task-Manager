using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeTaskManager.Api.Contracts.Development;
using HomeTaskManager.Api.Contracts.Users;
using HomeTaskManager.Application.UseCases.Users;

namespace HomeTaskManager.Api.Tests;

/// <summary>Verifies DevelopmentOnlyAttribute-gated endpoints return 404 outside Development, using HomeTaskManagerApiProductionFactory.</summary>
public sealed class DevelopmentEndpointsTests : IDisposable
{
    private readonly HomeTaskManagerApiProductionFactory _factory = new();
    private readonly HttpClient _client;

    public DevelopmentEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AllowAnonymousDevEndpoint_OutsideDevelopment_ReturnsNotFound()
    {
        var response = await _client.PostAsync("/api/development/ensure-admin-carry-over", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminOnlyDevEndpoint_OutsideDevelopment_ReturnsNotFoundEvenForAnAuthenticatedAdmin()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest($"Admin-{Guid.NewGuid():N}", true, 100));
        var admin = await createResponse.Content.ReadFromJsonAsync<UserProfileDto>(TestJson.Options);
        await _client.PostAsync($"/api/users/{admin!.Id}/select", content: null);

        var response = await _client.PostAsJsonAsync("/api/development/seed-history", new SeedHistoryRequest(1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
