using System.Numerics;
using FluentAssertions;
using VoxelEngine.Entity.AI;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Tests.Entity;

public class AnimalMovementStateMachineTests
{
    [Fact]
    public void Tick_TransitionsFromIdleToWanderAfterIdleTimerExpires()
    {
        // Arrange
        var stateMachine = CreateStateMachine(idleTimeMin: 1f, idleTimeMax: 1f, randomSeed: 1234);

        // Act
        var beforeExpiry = stateMachine.Tick(Vector3.Zero, threatPosition: null, deltaTime: 0.5f);
        var afterExpiry = stateMachine.Tick(Vector3.Zero, threatPosition: null, deltaTime: 0.5f);

        // Assert
        beforeExpiry.State.Should().Be(AnimalMovementState.Idle);
        afterExpiry.State.Should().Be(AnimalMovementState.Wander);
        stateMachine.CurrentTarget.Should().NotBeNull();
    }

    [Fact]
    public void ApplyMovementResult_ReturnsToIdleWhenWanderTargetIsReached()
    {
        // Arrange
        var stateMachine = CreateStateMachine(idleTimeMin: 0f, idleTimeMax: 0f, randomSeed: 42);
        var directive = stateMachine.Tick(Vector3.Zero, threatPosition: null, deltaTime: 0.01f);

        // Act
        stateMachine.ApplyMovementResult(stateMachine.CurrentTarget!.Value, directive, blocked: false);

        // Assert
        stateMachine.State.Should().Be(AnimalMovementState.Idle);
        stateMachine.CurrentTarget.Should().BeNull();
    }

    [Fact]
    public void ApplyMovementResult_ReturnsToIdleWhenWanderMovementIsBlocked()
    {
        // Arrange
        var stateMachine = CreateStateMachine(idleTimeMin: 0f, idleTimeMax: 0f, randomSeed: 42);
        var directive = stateMachine.Tick(Vector3.Zero, threatPosition: null, deltaTime: 0.01f);

        // Act
        stateMachine.ApplyMovementResult(Vector3.Zero, directive, blocked: true);

        // Assert
        stateMachine.State.Should().Be(AnimalMovementState.Idle);
        stateMachine.CurrentTarget.Should().BeNull();
    }

    [Fact]
    public void Tick_TransitionsToFleeWhenThreatEntersRadius_AndBackToIdleOutsideRadius()
    {
        // Arrange
        var stateMachine = CreateStateMachine(fleeRadius: 8f, randomSeed: 7);

        // Act
        var fleeDirective = stateMachine.Tick(Vector3.Zero, new Vector3(2f, 0f, 0f), deltaTime: 0.1f);
        var idleDirective = stateMachine.Tick(Vector3.Zero, new Vector3(20f, 0f, 0f), deltaTime: 0.1f);

        // Assert
        fleeDirective.State.Should().Be(AnimalMovementState.Flee);
        stateMachine.State.Should().Be(AnimalMovementState.Idle);
        idleDirective.State.Should().Be(AnimalMovementState.Idle);
    }

    [Fact]
    public void ApplyTimeOfDayActivity_SetsSleepState_AndTickKeepsAnimalStill()
    {
        // Arrange
        var stateMachine = CreateStateMachine();
        stateMachine.ApplyTimeOfDayActivity(EntityTimeOfDayActivity.Sleep);

        // Act
        var directive = stateMachine.Tick(Vector3.Zero, threatPosition: null, deltaTime: 0.1f);

        // Assert
        stateMachine.State.Should().Be(AnimalMovementState.Sleep);
        directive.State.Should().Be(AnimalMovementState.Sleep);
        directive.Speed.Should().Be(0f);
        directive.DesiredDirection.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Tick_WhenSleepingAndThreatAppears_WakesToIdleBeforeFleeing()
    {
        // Arrange
        var stateMachine = CreateStateMachine(fleeRadius: 8f, randomSeed: 7);
        stateMachine.ApplyTimeOfDayActivity(EntityTimeOfDayActivity.Sleep);

        // Act
        var wakeDirective = stateMachine.Tick(Vector3.Zero, new Vector3(2f, 0f, 0f), deltaTime: 0.1f);
        var fleeDirective = stateMachine.Tick(Vector3.Zero, new Vector3(2f, 0f, 0f), deltaTime: 0.1f);

        // Assert
        wakeDirective.State.Should().Be(AnimalMovementState.Idle);
        fleeDirective.State.Should().Be(AnimalMovementState.Flee);
    }

    [Fact]
    public void ApplyTimeOfDayActivity_SetsBurrowState()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act
        stateMachine.ApplyTimeOfDayActivity(EntityTimeOfDayActivity.Burrow);
        var directive = stateMachine.Tick(Vector3.Zero, threatPosition: null, deltaTime: 0.1f);

        // Assert
        stateMachine.State.Should().Be(AnimalMovementState.Burrow);
        directive.State.Should().Be(AnimalMovementState.Burrow);
        directive.Speed.Should().Be(0f);
    }

    private static AnimalMovementStateMachine CreateStateMachine(
        float idleTimeMin = 2f,
        float idleTimeMax = 6f,
        float fleeRadius = 8f,
        int randomSeed = 1)
        => new(
            new EntityBehaviourMetadata
            {
                MoveSpeed = 3f,
                FleeSpeed = 6f,
                FleeRadius = fleeRadius,
                IdleTimeMin = idleTimeMin,
                IdleTimeMax = idleTimeMax,
                WanderRadius = 12f,
                DayActivity = EntityTimeOfDayActivity.Active,
                NightActivity = EntityTimeOfDayActivity.Sleep
            },
            new Random(randomSeed));
}
