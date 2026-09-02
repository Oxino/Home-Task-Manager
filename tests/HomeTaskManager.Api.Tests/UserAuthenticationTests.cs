using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeTaskManager.Api.Contracts.Users;
using HomeTaskManager.Application.UseCases.Users;

namespace HomeTaskManager.Api.Tests;

public sealed class UserAuthenticationTests : IDisposable
{
    private readonly HomeTaskManagerApiFactory _factory = new();
    private readonly HttpClient _client;

    public UserAuthenticationTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateProfile_Select_ThenGetCurrent_ReturnsSameAuthenticatedProfile()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest($"Julie-{Guid.NewGuid():N}", false, 100));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var profile = await createResponse.Content.ReadFromJsonAsync<UserProfileDto>(TestJson.Options);

        var selectResponse = await _client.PostAsync($"/api/users/{profile!.Id}/select", content: null);
        selectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentResponse = await _client.GetAsync("/api/users/current");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await currentResponse.Content.ReadFromJsonAsync<UserProfileDto>(TestJson.Options);
        current!.Id.Should().Be(profile.Id);
    }

    [Fact]
    public async Task AnonymousCanSelfAttributeAdminRole_OnlyForTheFirstProfile()
    {
        var firstAdmin = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest($"Admin-{Guid.NewGuid():N}", true, 100));
        firstAdmin.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondAdmin = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest($"Admin2-{Guid.NewGuid():N}", true, 100));
        secondAdmin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_Request_To_Protected_Endpoint_Returns401()
    {
        var response = await _client.GetAsync("/api/users/current");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NonAdmin_Cannot_Access_AdminOnly_Endpoint()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest($"Participant-{Guid.NewGuid():N}", false, 100));
        var profile = await createResponse.Content.ReadFromJsonAsync<UserProfileDto>(TestJson.Options);
        await _client.PostAsync($"/api/users/{profile!.Id}/select", content: null);

        var response = await _client.GetAsync("/api/users/all");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
