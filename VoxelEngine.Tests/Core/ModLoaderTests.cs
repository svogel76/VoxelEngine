using System.Reflection;
using FluentAssertions;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Core;

namespace VoxelEngine.Tests.Core;

public sealed class ModLoaderTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"VoxelEngine.ModLoader.{Guid.NewGuid():N}");

    [Fact]
    public void LoadAll_EmptyModsDirectory_ReturnsEmptyList()
    {
        Directory.CreateDirectory(_tempRoot);

        var loader = CreateLoader();
        var mods = loader.LoadAll(_tempRoot);

        mods.Should().BeEmpty();
    }

    [Fact]
    public void LoadAll_SkipsFolderWithoutModJson()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "NoManifest"));

        var loader = CreateLoader();
        var mods = loader.LoadAll(_tempRoot);

        mods.Should().BeEmpty();
    }

    [Fact]
    public void LoadAll_CircularDependency_Throws()
    {
        CreateMod("A", "a", typeof(MockModA).FullName!, ["b"]);
        CreateMod("B", "b", typeof(MockModB).FullName!, ["a"]);

        var loader = CreateLoader();
        Action act = () => loader.LoadAll(_tempRoot);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Circular*")
            .WithMessage("*a*")
            .WithMessage("*b*");
    }

    [Fact]
    public void LoadAll_MissingDependency_Throws()
    {
        CreateMod("A", "a", typeof(MockModA).FullName!, ["missing"]);

        var loader = CreateLoader();
        Action act = () => loader.LoadAll(_tempRoot);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*a*")
            .WithMessage("*missing*");
    }

    [Fact]
    public void LoadAll_DependencyOrder_LoadsDependenciesFirst()
    {
        CreateMod("A", "a", typeof(MockModA).FullName!, ["b"]);
        CreateMod("B", "b", typeof(MockModB).FullName!, []);

        var loader = CreateLoader();
        IReadOnlyList<IGameMod> mods = loader.LoadAll(_tempRoot);

        mods.Select(mod => mod.Id).Should().Equal(["b", "a"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private ModLoader CreateLoader()
    {
        return new ModLoader(
            assemblyLoader: _ => typeof(ModLoaderTests).Assembly,
            instanceFactory: (_, entryClass) => entryClass switch
            {
                var name when name == typeof(MockModA).FullName => new MockModA(),
                var name when name == typeof(MockModB).FullName => new MockModB(),
                _ => throw new InvalidOperationException($"Unknown test entry class '{entryClass}'.")
            });
    }

    private void CreateMod(string folderName, string id, string entryClass, IReadOnlyList<string> dependencies)
    {
        string modDirectory = Path.Combine(_tempRoot, folderName);
        Directory.CreateDirectory(modDirectory);

        File.WriteAllText(
            Path.Combine(modDirectory, "mod.json"),
            $$"""
            {
              "id": "{{id}}",
              "name": "{{folderName}}",
              "version": "1.0.0",
              "entry_class": "{{entryClass}}",
              "dependencies": [{{string.Join(", ", dependencies.Select(dep => $"\"{dep}\""))}}]
            }
            """);

        File.WriteAllText(Path.Combine(modDirectory, $"{id}.dll"), string.Empty);
    }

    private sealed class MockModA : TestModBase
    {
        public override string Id => "a";
    }

    private sealed class MockModB : TestModBase
    {
        public override string Id => "b";
    }

    private abstract class TestModBase : IGameMod
    {
        public abstract string Id { get; }

        public void RegisterComponents(IComponentRegistry registry)
        {
        }

        public void RegisterBehaviours(IBehaviourRegistry registry)
        {
        }

        public void RegisterBlocks(IBlockRegistry registry)
        {
        }

        public void Initialize(IModContext context)
        {
        }

        public void Update(double deltaTime)
        {
        }

        public void Render(double deltaTime)
        {
        }

        public void Shutdown()
        {
        }
    }
}
