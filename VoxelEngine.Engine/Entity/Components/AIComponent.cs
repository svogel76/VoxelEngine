using System.Numerics;
using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.Components;

public sealed class AIComponent : IComponent
{
    private readonly IBehaviourNode _tree;

    public AIComponent(IBehaviourNode tree, float yawRadians = 0f)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        YawRadians = yawRadians;
    }

    public string ComponentId => "ai";
    public float YawRadians { get; private set; }
    public NodeResult LastResult { get; private set; }

    public void Update(IEntity entity, IModContext context, double deltaTime)
        => LastResult = _tree.Tick(entity, context, deltaTime);

    internal void SetYawFromDisplacement(Vector2 displacement)
    {
        if (displacement.LengthSquared() <= 0.000001f)
            return;

        YawRadians = MathF.Atan2(displacement.X, displacement.Y);
    }

    public static AIComponent FromJson(JsonElement config, IBehaviourRegistry registry)
    {
        if (!config.TryGetProperty("behaviour_tree", out var behaviourTree))
            throw new InvalidOperationException("AI component configuration is missing required property 'behaviour_tree'.");

        return new AIComponent(BehaviourTreeLoader.Load(behaviourTree, registry));
    }
}
