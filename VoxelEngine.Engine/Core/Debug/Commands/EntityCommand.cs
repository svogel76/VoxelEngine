using VoxelEngine.Entity.Components;
using VoxelEngine.Entity.Models;
using EntityType = global::VoxelEngine.Entity.Entity;

namespace VoxelEngine.Core.Debug.Commands;

public sealed class EntityCommand : ICommand
{
    public string Name => "entity";
    public string Description => "Spawnt oder listet Debug-Entities";
    public string Usage => "entity spawn <modelId> | entity list";

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
        if (args.Length < 2)
        {
            LogAvailableModels(context);
            return;
        }

        string requestedModelId = args[1];
        var model = context.EntityModels
            .GetAllModels()
            .FirstOrDefault(m => string.Equals(m.Id, requestedModelId, StringComparison.OrdinalIgnoreCase));

        if (model is null)
        {
            context.Console.Log($"Unbekanntes Entity-Modell: '{requestedModelId}'.");
            LogAvailableModels(context);
            return;
        }

        var spawnPosition = CalculateSpawnPosition(context, model);
        var entity = BuildDebugEntity(model, spawnPosition, context);

        context.EntityManager.Add(entity);
        context.Console.Log($"Entity '{model.Id}' gespawnt bei ({entity.InternalPosition.X:F2}, {entity.InternalPosition.Y:F2}, {entity.InternalPosition.Z:F2}).");
    }

    private static EntityType BuildDebugEntity(
        IVoxelModelDefinition model,
        System.Numerics.Vector3 spawnPosition,
        GameContext context)
    {
        var entity = new EntityType(model.Id, spawnPosition);

        float width = model.PlacementBounds.Max.X - model.PlacementBounds.Min.X;
        float height = model.PlacementBounds.Max.Y - model.PlacementBounds.Min.Y;
        var phys = new PhysicsComponent(
            context.World,
            width,
            height,
            context.Settings.Gravity,
            context.Settings.MaxFallSpeed,
            context.Settings.FallDamageThreshold,
            context.Settings.FallDamageMultiplier);
        entity.AddComponent(phys);

        if (model.Metadata.Ai?.HasBehaviourTree == true)
        {
            var tree = BehaviourTreeLoader.Load(model.Metadata.Ai.BehaviourTree, EngineModContext.BehaviourTreeRegistry);
            entity.AddComponent(new AIComponent(tree));
        }

        entity.AddComponent(new RenderComponent(model.Id, model.Metadata.Display?.Scale ?? 1f));
        entity.IsActive = true;
        return entity;
    }

    private static void LogAvailableModels(GameContext context)
    {
        string[] modelIds = context.EntityModels
            .GetAllModels()
            .Select(m => m.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        context.Console.Log(modelIds.Length == 0
            ? "Keine Entity-Modelle geladen."
            : $"Verwendung: entity spawn <modelId>. Verfuegbar: {string.Join(", ", modelIds)}");
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

        return context.Player.InternalPosition + horizontalForward * spawnDistance;
    }
}
