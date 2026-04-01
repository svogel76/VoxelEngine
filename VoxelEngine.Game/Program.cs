using VoxelEngine.Core;

namespace VoxelEngine.Game;

public static class Program
{
    public static void Main()
    {
        var mods = new ModLoader().LoadAll("Mods/");
        new EngineRunner().Run(mods);
    }
}
