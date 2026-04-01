using System.Text.Json;
using FluentAssertions;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Api.World;
using VoxelEngine.Entity.BehaviourTree;
using VoxelEngine.Entity.BehaviourTree.Conditions;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Tests.Entity.BehaviourTree;

public class BehaviourTreeTests
{
    [Fact]
    public void Selector_ReturnsSuccess_WhenFirstChildSucceeds()
    {
        var selector = new Selector([new StubNode(NodeResult.Success), new StubNode(NodeResult.Failure)]);

        var result = selector.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Success);
    }

    [Fact]
    public void Selector_ReturnsFailure_WhenAllChildrenFail()
    {
        var selector = new Selector([new StubNode(NodeResult.Failure), new StubNode(NodeResult.Failure)]);

        var result = selector.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Failure);
    }

    [Fact]
    public void Selector_ReturnsRunning_WhenCurrentChildIsRunning()
    {
        var selector = new Selector([new StubNode(NodeResult.Running), new StubNode(NodeResult.Success)]);

        var result = selector.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Running);
    }

    [Fact]
    public void Sequence_ReturnsFailure_WhenFirstChildFails()
    {
        var sequence = new Sequence([new StubNode(NodeResult.Failure), new StubNode(NodeResult.Success)]);

        var result = sequence.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Failure);
    }

    [Fact]
    public void Sequence_ReturnsSuccess_WhenAllChildrenSucceed()
    {
        var sequence = new Sequence([new StubNode(NodeResult.Success), new StubNode(NodeResult.Success)]);

        var result = sequence.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Success);
    }

    [Fact]
    public void BehaviourTreeLoader_ParsesSelectorWithTwoActionChildren()
    {
        var registry = new VoxelEngine.BehaviourRegistry();
        registry.RegisterAction("idle", config => new StubNode(NodeResult.Success));
        registry.RegisterAction("wander", config => new StubNode(NodeResult.Failure));

        JsonElement tree = JsonDocument.Parse(
            """
            {
              "type": "selector",
              "children": [
                { "type": "action", "name": "idle", "duration_seconds": 1.0 },
                { "type": "action", "name": "wander", "speed": 1.5, "radius": 5.0, "pause_seconds": 2.0 }
              ]
            }
            """).RootElement.Clone();

        var node = VoxelEngine.BehaviourTreeLoader.Load(tree, registry);
        var result = node.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Success);
    }

    [Fact]
    public void BehaviourTreeLoader_ThrowsOnUnknownNodeType()
    {
        var registry = new VoxelEngine.BehaviourRegistry();
        JsonElement tree = JsonDocument.Parse("{ \"type\": \"mystery\" }").RootElement.Clone();

        Action act = () => VoxelEngine.BehaviourTreeLoader.Load(tree, registry);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mystery*");
    }

    [Fact]
    public void HealthLowCondition_ReturnsSuccess_BelowThreshold()
    {
        var entity = CreateEntity();
        var health = new HealthComponent(10f);
        health.TakeDamage(8f);
        entity.AddComponent(health);
        var condition = new HealthLowCondition(0.3f);

        var result = condition.Tick(entity, CreateContext(), 0.1);

        result.Should().Be(NodeResult.Success);
    }

    [Fact]
    public void HealthLowCondition_ReturnsFailure_AboveThreshold()
    {
        var entity = CreateEntity();
        var health = new HealthComponent(10f);
        health.TakeDamage(1f);
        entity.AddComponent(health);
        var condition = new HealthLowCondition(0.3f);

        var result = condition.Tick(entity, CreateContext(), 0.1);

        result.Should().Be(NodeResult.Failure);
    }

    [Fact]
    public void HealthLowCondition_ReturnsFailure_WhenHealthComponentAbsent()
    {
        var condition = new HealthLowCondition(0.3f);

        var result = condition.Tick(CreateEntity(), CreateContext(), 0.1);

        result.Should().Be(NodeResult.Failure);
    }

    private static global::VoxelEngine.Entity.Entity CreateEntity()
        => new("test", System.Numerics.Vector3.Zero);

    private static TestModContext CreateContext()
        => new();

    private sealed class StubNode : IBehaviourNode
    {
        private readonly NodeResult _result;

        public StubNode(NodeResult result)
        {
            _result = result;
        }

        public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
            => _result;
    }

    private sealed class TestModContext : IModContext
    {
        public string ModId => "test";
        public string AssetBasePath => Path.GetFullPath(Path.Combine("Mods", "Test", "Assets"));
        public IComponentRegistry ComponentRegistry => throw new NotSupportedException();
        public IBehaviourRegistry BehaviourRegistry => throw new NotSupportedException();
        public IBlockRegistry BlockRegistry => throw new NotSupportedException();
        public IWorldAccess World => throw new NotSupportedException();
        public IInputState Input => throw new NotSupportedException();
        public IKeyBindings KeyBindings => throw new NotSupportedException();
        public IEntity Player { get; } = new global::VoxelEngine.Entity.Entity("player", System.Numerics.Vector3.Zero);
        public double WorldTimeHours => 12.0;
        public bool IsDay => true;
        public bool IsNight => false;
    }
}

