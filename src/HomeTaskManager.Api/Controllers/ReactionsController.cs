using HomeTaskManager.Application.UseCases.TaskInstances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeTaskManager.Api.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public sealed class ReactionsController(
    IAddReactionUseCase addReactionUseCase,
    IRemoveReactionUseCase removeReactionUseCase,
    IListReactionsForCommentUseCase listReactionsUseCase) : ControllerBase
{
    [HttpPost("{commentId}/reactions")]
    public async Task<ActionResult<object>> AddReaction(Guid commentId, [FromBody] AddReactionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (!Guid.TryParse(userId, out var parsedUserId)) return Unauthorized();

        var reaction = await addReactionUseCase.ExecuteAsync(commentId, parsedUserId, request.ReactionType, cancellationToken);
        return Ok(reaction);
    }

    [HttpDelete("reactions/{reactionId}")]
    public async Task<IActionResult> RemoveReaction(Guid reactionId, CancellationToken cancellationToken)
    {
        await removeReactionUseCase.ExecuteAsync(reactionId, cancellationToken);
        return Ok();
    }

    [HttpGet("{commentId}/reactions")]
    public async Task<ActionResult<object>> ListReactions(Guid commentId, CancellationToken cancellationToken)
    {
        var reactions = await listReactionsUseCase.ExecuteAsync(commentId, cancellationToken);
        return Ok(reactions);
    }
}

public sealed record AddReactionRequest(string ReactionType);
