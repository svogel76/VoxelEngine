using System.Numerics;
using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Entity.BehaviourTree;

internal static class BehaviourNodeUtilities
{
    private const float MovementEpsilon = 0.000001f;
    private const float ArrivalDistance = 0.35f;

    public static float ReadRequiredSingle(JsonElement config, string propertyName)
    {
        if (!config.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"Behaviour node '{config.GetProperty("name").GetString()}' is missing required property '{propertyName}'.");

        return property.GetSingle();
    }

    public static Vector3 GetEntityPosition(IEntity entity)
        => new(entity.Position.X, entity.Position.Y, entity.Position.Z);

    public static Vector3 GetPlayerPosition(IModContext context)
        => new(context.Player.Position.X, context.Player.Position.Y, context.Player.Position.Z);

    public static float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    public static bool HasReachedTarget(Vector3 position, Vector3 target)
        => GetHorizontalDistance(position, target) <= ArrivalDistance;

    public static Vector3 RandomHorizontalOffset(Random random, float radius)
    {
        double angle = random.NextDouble() * Math.PI * 2.0;
        double distance = Math.Sqrt(random.NextDouble()) * radius;
        return new Vector3(
            (float)(Math.Cos(angle) * distance),
            0f,
            (float)(Math.Sin(angle) * distance));
    }

    public static void Stop(IEntity entity, double deltaTime)
        => Move(entity, Vector3.Zero, 0f, deltaTime);

    public static PhysicsComponent.HorizontalMovementResult Move(IEntity entity, Vector3 desiredDirection, float speed, double deltaTime)
    {
        if (entity is not Entity engineEntity)
            return default;

        var physics = entity.GetComponent<PhysicsComponent>();
        if (physics is null)
            return default;

        Vector2 desiredVelocity = desiredDirection.LengthSquared() > 0f
            ? new Vector2(desiredDirection.X, desiredDirection.Z) * speed
            : Vector2.Zero;

        var movement = physics.MoveHorizontally(engineEntity, desiredVelocity, deltaTime);
        if (movement.Displacement.LengthSquared() > MovementEpsilon)
            entity.GetComponent<AIComponent>()?.SetYawFromDisplacement(movement.Displacement);

        return movement;
    }
}

