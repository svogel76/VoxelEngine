using VoxelEngine.Core;

string modsDirectory = ResolveModsDirectory();
var mods = new ModLoader().LoadAll(modsDirectory);
new EngineRunner().Run(mods);

static string ResolveModsDirectory()
{
    // Launcher output is "Run/", while mods live one level above in "Mods/".
    string baseDirectoryMods = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Mods"));
    if (Directory.Exists(baseDirectoryMods))
        return baseDirectoryMods;

    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Mods"));
}
