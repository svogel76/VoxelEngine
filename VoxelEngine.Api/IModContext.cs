using VoxelEngine.Api.Entity;

namespace VoxelEngine.Api;

public interface IModContext : IGameContext
{
    string ModId { get; }
    IComponentRegistry ComponentRegistry { get; }
    IBehaviourRegistry BehaviourRegistry { get; }
    double WorldTimeHours { get; }
    bool IsDay { get; }
    bool IsNight { get; }
}
