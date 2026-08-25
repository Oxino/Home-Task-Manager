using FluentAssertions;
using HomeTaskManager.Application.UseCases.TaskInstances;

namespace HomeTaskManager.Application.Tests;

public sealed class ReactionUseCasesTests
{
    [Fact]
    public async Task AddReaction_creates_new_reaction_idempotently()
    {
        var reactions = new InMemoryReactionRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var addUseCase = new AddReactionUseCase(reactions, unitOfWork);

        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var r1 = await addUseCase.ExecuteAsync(commentId, userId, "👍", default);
        var r2 = await addUseCase.ExecuteAsync(commentId, userId, "👍", default);

        r1.Id.Should().Be(r2.Id);
        r1.ReactionType.Should().Be("👍");
    }

    [Fact]
    public async Task ListReactions_returns_all_reactions_for_comment()
    {
        var reactions = new InMemoryReactionRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var addUseCase = new AddReactionUseCase(reactions, unitOfWork);
        var listUseCase = new ListReactionsForCommentUseCase(reactions);

        var commentId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await addUseCase.ExecuteAsync(commentId, user1, "👍", default);
        await addUseCase.ExecuteAsync(commentId, user2, "❤️", default);

        var listed = await listUseCase.ExecuteAsync(commentId, default);

        listed.Should().HaveCount(2);
        listed.Should().Contain(r => r.ReactionType == "👍" && r.UserId == user1);
        listed.Should().Contain(r => r.ReactionType == "❤️" && r.UserId == user2);
    }

    [Fact]
    public async Task RemoveReaction_deletes_reaction()
    {
        var reactions = new InMemoryReactionRepository();
        var unitOfWork = new NoOpUnitOfWork();
        var addUseCase = new AddReactionUseCase(reactions, unitOfWork);
        var removeUseCase = new RemoveReactionUseCase(reactions, unitOfWork);
        var listUseCase = new ListReactionsForCommentUseCase(reactions);

        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var reaction = await addUseCase.ExecuteAsync(commentId, userId, "👍", default);
        await removeUseCase.ExecuteAsync(reaction.Id, default);

        var remaining = await listUseCase.ExecuteAsync(commentId, default);
        remaining.Should().BeEmpty();
    }
}
