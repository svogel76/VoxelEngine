using System.Runtime.CompilerServices;
using VoxelEngine.World;

namespace VoxelEngine.Tests;

internal static class TestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        BlockRegistry.Clear();
        DefaultBlockRegistration.RegisterDefaults(BlockRegistryAdapter.Instance);
    }
}