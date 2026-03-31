using FluentAssertions;
using Silk.NET.Maths;
using VoxelEngine.Core;
using VoxelEngine.Game;
using VoxelEngine.Core.Debug.Commands;
using VoxelEngine.Entity.Models;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Core.Debug.Commands;

public class EntityCommandTests
{
    [Fact]
    public void Execute_Spawn_UsesRequestedModelId()
    {
        // Arrange
        var context = CreateContext(["test", "deer"]);
        var command = new EntityCommand();

        // Act
        command.Execute(["spawn", "deer"], context);

        // Assert
        context.EntityManager.Count.Should().Be(1);
        context.Console.GetOutput().Should().ContainSingle(message => message.Contains("Entity 'deer' gespawnt", StringComparison.Ordinal));
    }

    [Fact]
    public void Execute_Spawn_WithUnknownModel_LogsAvailableModels()
    {
        // Arrange
        var context = CreateContext(["test", "deer"]);
        var command = new EntityCommand();

        // Act
        command.Execute(["spawn", "wolf"], context);

        // Assert
        context.EntityManager.Count.Should().Be(0);
        context.Console.GetOutput().Should().Contain(message => message.Contains("Unbekanntes Entity-Modell: 'wolf'.", StringComparison.Ordinal));
        context.Console.GetOutput().Should().Contain(message => message.Contains("Verfuegbar: deer, test", StringComparison.Ordinal));
    }

    private static GameContext CreateContext(params string[] modelIds)
    {
        var settings = new EngineSettings();
        var world = new global::VoxelEngine.World.World();
        var player = new Player(new System.Numerics.Vector3(0f, 64f, 0f));
        var camera = new Camera(new Vector3D<float>(player.EyePosition.X, player.EyePosition.Y, player.EyePosition.Z), 16f / 9f, settings);
        var generator = new WorldGenerator(settings);
        var persistence = new InMemoryPersistence();

        return new GameContext(
            settings,
            new KeyBindings(),
            world,
            player,
            camera,
            renderer: null!,
            inputHandler: null!,
            generator,
            new TestModelLibrary(modelIds),
            persistence);
    }

    private sealed class TestModelLibrary : IEntityModelLibrary
    {
        private readonly Dictionary<string, IVoxelModelDefinition> _models;

        public TestModelLibrary(IEnumerable<string> modelIds)
        {
            _models = modelIds.ToDictionary(
                id => id,
                CreateModel,
                StringComparer.OrdinalIgnoreCase);
        }

        public EntityAtlasDefinition Atlas { get; } = new("Assets/Entities/entity_atlas.png", 4, 2);

        public IReadOnlyCollection<IVoxelModelDefinition> GetAllModels()
            => _models.Values;

        public IVoxelModelDefinition GetModel(string modelId)
            => _models[modelId];

        private static IVoxelModelDefinition CreateModel(string id)
            => new VoxelModelDefinition(
                id,
                0.25f,
                [new VoxelModelVoxel(0, 0, 0, 0, 0, VoxelTint.White)]);
    }
}

