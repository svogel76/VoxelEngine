using VoxelEngine.Entity;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Core.Debug.Commands;

public sealed class EntityCommand : ICommand
{
    public string Name => "entity";
    public string Description => "Spawnt oder listet Debug-Entities";
    public string Usage => "entity spawn test | entity list";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length == 0)
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "spawn":
                SpawnEntity(args, context);
                break;

            case "list":
                context.Console.Log($"Entities aktiv: {context.EntityManager.Count}");
                break;

            default:
                context.Console.Log($"Verwendung: {Usage}");
                break;
        }
    }

    private static void SpawnEntity(string[] args, GameContext context)
    {
        if (args.Length < 2 || !string.Equals(args[1], "test", StringComparison.OrdinalIgnoreCase))
        {
            context.Console.Log("Aktuell verfuegbar: entity spawn test");
            return;
        }

        var model = context.EntityModels.GetModel("test");
        float yawRadians = context.Camera.Yaw * MathF.PI / 180f;
        var entity = new TestVoxelEntity(CalculateSpawnPosition(context, model), model, yawRadians);
        context.EntityManager.Add(entity);

        context.Console.Log($"Test-Entity gespawnt bei ({entity.Position.X:F2}, {entity.Position.Y:F2}, {entity.Position.Z:F2}).");
    }

    private static System.Numerics.Vector3 CalculateSpawnPosition(GameContext context, IVoxelModelDefinition model)
    {
        var front = context.Camera.Front;
        var horizontalForward = new System.Numerics.Vector3(front.X, 0f, front.Z);
        if (horizontalForward.LengthSquared() < 0.0001f)
            horizontalForward = new System.Numerics.Vector3(0f, 0f, -1f);
        else
            horizontalForward = System.Numerics.Vector3.Normalize(horizontalForward);

        float modelWidth = model.PlacementBounds.Max.X - model.PlacementBounds.Min.X;
        float modelDepth = model.PlacementBounds.Max.Z - model.PlacementBounds.Min.Z;
        float spawnDistance = MathF.Max(3.0f, MathF.Max(modelWidth, modelDepth) * 0.75f + 1.5f);

        return context.Player.Position + horizontalForward * spawnDistance;
    }
}
