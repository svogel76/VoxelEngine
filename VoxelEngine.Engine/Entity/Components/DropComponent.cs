using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.Components;

public sealed record DropEntry(string Item, int Count, float Chance);

/// <summary>
/// Definiert, welche Items beim Tod der Entity fallen gelassen werden.
/// </summary>
public sealed class DropComponent : IComponent
{
    public string ComponentId => "drops";

    public IReadOnlyList<DropEntry> Drops { get; }

    public DropComponent(IReadOnlyList<DropEntry> drops)
    {
        Drops = drops ?? [];
    }

    public void Update(IEntity entity, IModContext context, double deltaTime)
    {
        // Drops werden beim Tod ausgelöst – kein Update-Tick nötig.
    }

    public static DropComponent FromJson(JsonElement config)
    {
        var drops = new List<DropEntry>();

        if (config.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in config.EnumerateArray())
            {
                string item   = element.TryGetProperty("item",   out var ip) ? ip.GetString() ?? "" : "";
                int    count  = element.TryGetProperty("count",  out var cp) ? cp.GetInt32()        : 1;
                float  chance = element.TryGetProperty("chance", out var ch) ? ch.GetSingle()       : 1f;
                drops.Add(new DropEntry(item, count, chance));
            }
        }

        return new DropComponent(drops);
    }
}
