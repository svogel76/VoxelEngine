using VoxelEngine.Core;
using VoxelEngine.Game;

var settings = EngineSettings.LoadFrom("Assets/");
var bindings = KeyBindingLoader.LoadFrom("Assets/");
new EngineRunner(settings, bindings).Run(new VoxelGame());
