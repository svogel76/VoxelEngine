using VoxelEngine.Core;

var mods = new ModLoader().LoadAll("Mods/");
new EngineRunner().Run(mods);
