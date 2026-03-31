using System.Text.Json;

namespace VoxelEngine.Api.Entity;

public interface IComponentRegistry
{
    void Register(string name, Func<JsonElement, IComponent> factory);
    IComponent Create(string name, JsonElement config);
}
