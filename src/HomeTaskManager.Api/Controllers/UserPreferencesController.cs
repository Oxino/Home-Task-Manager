using HomeTaskManager.Api;
using HomeTaskManager.Api.Contracts.Users;
using HomeTaskManager.Application.UseCases.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

/// <summary>Reads/updates the caller's own display and notification preferences.</summary>
[ApiController]
[Route("api/users/preferences")]
[Authorize]
public sealed class UserPreferencesController(IUserPreferencesUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserPreferencesDto>> Get(CancellationToken cancellationToken) =>
        Ok(await useCase.GetAsync(User.GetCurrentUserId(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<UserPreferencesDto>> Update(UpdateUserPreferencesRequest request, CancellationToken cancellationToken) =>
        Ok(await useCase.UpdateAsync(new UpdateUserPreferencesCommand(User.GetCurrentUserId(), request.NotificationPreference, request.DateDisplayFormat), cancellationToken));
}