using System.Runtime.CompilerServices;
using VoxelEngine.Game.Blocks;
using VoxelEngine.World;

namespace VoxelEngine.Tests;

internal static class TestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        BlockRegistry.Clear();
        new BlockDefinitionLoader("Assets/").LoadInto(BlockRegistryAdapter.Instance);
    }
}
