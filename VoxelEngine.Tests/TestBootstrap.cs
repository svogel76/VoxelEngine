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

        string candidate = Path.GetFullPath(Path.Combine("Mods", "VoxelGame", "Assets"));
        if (!Directory.Exists(candidate))
        {
            candidate = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "Mods", "VoxelGame", "Assets"));
        }

        new BlockDefinitionLoader(candidate).LoadInto(BlockRegistryAdapter.Instance);
    }
}
