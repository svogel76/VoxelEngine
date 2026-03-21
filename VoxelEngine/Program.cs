using VoxelEngine.Core;

var settings = new EngineSettings
{
    TargetFPS = 60
};

using var engine = new Engine(settings);
engine.Run();