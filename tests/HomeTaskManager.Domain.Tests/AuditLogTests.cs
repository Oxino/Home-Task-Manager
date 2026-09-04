using FluentAssertions;
using HomeTaskManager.Domain.Entities;
using HomeTaskManager.Domain.Exceptions;

namespace HomeTaskManager.Domain.Tests;

/// <summary>Validates the AuditLog entity's construction rules (required entity type/action).</summary>
public class AuditLogTests
{
    [Fact]
    public void Records_the_entity_action_and_actor()
    {
        var entityId = Guid.NewGuid();
        var performedByUserId = Guid.NewGuid();

        var entry = new AuditLog("TaskTemplate", entityId, "Created", performedByUserId, null, "Title=Aspirateur");

        entry.EntityType.Should().Be("TaskTemplate");
        entry.EntityId.Should().Be(entityId);
        entry.Action.Should().Be("Created");
        entry.PerformedByUserId.Should().Be(performedByUserId);
        entry.NewValue.Should().Be("Title=Aspirateur");
    }

    [Fact]
    public void Rejects_a_blank_entity_type()
    {
        var action = () => new AuditLog(" ", Guid.NewGuid(), "Created", Guid.NewGuid(), null, null);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Rejects_a_blank_action()
    {
        var action = () => new AuditLog("User", Guid.NewGuid(), " ", Guid.NewGuid(), null, null);

        action.Should().Throw<DomainValidationException>();
    }
}
