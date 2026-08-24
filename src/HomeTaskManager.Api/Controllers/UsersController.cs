using HomeTaskManager.Api.Contracts.Users;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(
    ICreateUserUseCase createUserUseCase,
    IListActiveProfilesUseCase listActiveProfilesUseCase,
    ISelectProfileUseCase selectProfileUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetActiveProfiles(CancellationToken cancellationToken)
    {
        var profiles = await listActiveProfilesUseCase.ExecuteAsync(cancellationToken);
        return Ok(profiles);
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileDto>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateUserCommand(request.DisplayName, request.IsAdmin, request.AvailabilityPercentage);
            var profile = await createUserUseCase.ExecuteAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetActiveProfiles), new { }, profile);
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/select")]
    public async Task<ActionResult<UserProfileDto>> SelectProfile(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await selectProfileUseCase.ExecuteAsync(id, cancellationToken);
            return Ok(profile);
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
