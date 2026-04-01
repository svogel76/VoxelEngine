using System.Text.Json;

namespace VoxelEngine.Api.Entity;

public interface IBehaviourRegistry
{
    void RegisterCondition(string name, Func<JsonElement, IBehaviourNode> factory);
    void RegisterAction(string name, Func<JsonElement, IBehaviourNode> factory);
    IBehaviourNode Create(string type, string name, JsonElement config);
}
