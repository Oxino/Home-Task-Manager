using HomeTaskManager.Api.Contracts.Users;
using HomeTaskManager.Application.UseCases.Users;
using HomeTaskManager.Application.Exceptions;
using HomeTaskManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using HomeTaskManager.Application.Abstractions.Repositories;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(
    ICreateUserUseCase createUserUseCase,
    IListActiveProfilesUseCase listActiveProfilesUseCase,
    ISelectProfileUseCase selectProfileUseCase,
    IUserRepository userRepository,
    IUpdateUserUseCase updateUserUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetActiveProfiles(CancellationToken cancellationToken)
    {
        var profiles = await listActiveProfilesUseCase.ExecuteAsync(cancellationToken);
        return Ok(profiles);
    }

    [HttpGet("all")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return Ok(users.Select(user => new UserProfileDto(user.Id, user.DisplayName, user.IsAdmin, user.IsActive, user.AvailabilityPercentage)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileDto>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var requestingUserIsAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
            var command = new CreateUserCommand(request.DisplayName, request.IsAdmin, request.AvailabilityPercentage, requestingUserIsAdmin);
            var profile = await createUserUseCase.ExecuteAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetActiveProfiles), new { }, profile);
        }
        catch (ForbiddenOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
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
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()),
                new Claim(ClaimTypes.Name, profile.DisplayName),
                new Claim(ClaimTypes.Role, profile.IsAdmin ? "Admin" : "Participant"),
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Ok(profile);
        }
        catch (DomainValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("current")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<UserProfileDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(User.GetCurrentUserId(), cancellationToken);
        return user is null || !user.IsActive ? Unauthorized() : Ok(new UserProfileDto(user.Id, user.DisplayName, user.IsAdmin, user.IsActive, user.AvailabilityPercentage));
    }

    [HttpPut("{id:guid}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileDto>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await updateUserUseCase.ExecuteAsync(new UpdateUserCommand(id, User.GetCurrentUserId(), request.DisplayName, request.IsAdmin, request.AvailabilityPercentage, request.IsActive), cancellationToken);
            return Ok(result);
        }
        catch (ForbiddenOperationException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (DomainValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
