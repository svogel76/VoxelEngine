using System.Numerics;
using FluentAssertions;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Tests.Entity;

public class ComponentTests
{
    [Fact]
    public void HealthComponent_TakeDamage_ReducesCurrentHp()
    {
        // Arrange
        var entity = new global::VoxelEngine.Entity.Entity("test", Vector3.Zero);
        var health = new HealthComponent(20f);
        entity.AddComponent(health);

        // Act
        health.TakeDamage(5f);

        // Assert
        health.CurrentHp.Should().Be(15f);
        health.IsDead.Should().BeFalse();
    }

    [Fact]
    public void HealthComponent_TakeDamage_DiesAtZeroHp()
    {
        // Arrange
        var entity = new global::VoxelEngine.Entity.Entity("test", Vector3.Zero);
        var health = new HealthComponent(10f);
        entity.AddComponent(health);

        // Act
        health.TakeDamage(10f);

        // Assert
        health.CurrentHp.Should().Be(0f);
        health.IsDead.Should().BeTrue();
    }

    [Fact]
    public void Entity_GetComponent_ReturnsNullForUnregisteredComponent()
    {
        // Arrange
        var entity = new global::VoxelEngine.Entity.Entity("test", Vector3.Zero);

        // Act
        var result = entity.GetComponent<HealthComponent>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ComponentRegistry_Create_ThrowsForUnknownComponentName()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        var act = () => registry.Create("nonexistent", default);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ComponentRegistry_RegisterAndCreate_ResolvesCustomComponent()
    {
        // Arrange
        var registry = new ComponentRegistry();
        var created  = new StubComponent();
        registry.Register("stub", _ => created);

        // Act
        var result = registry.Create("stub", default);

        // Assert
        result.Should().BeSameAs(created);
    }

    private sealed class StubComponent : IComponent
    {
        public string ComponentId => "stub";

        public void Update(IEntity entity, IModContext context, double deltaTime) { }
    }
}
