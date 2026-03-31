using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Entity.Components;

/// <summary>
/// Render-Daten einer Entity: Modell-Referenz, Skalierung und YawRadians für den EntityRenderer.
/// </summary>
public sealed class RenderComponent : IComponent
{
    public string ComponentId => "render";

    public string ModelId   { get; }
    public float  Scale     { get; }
    public bool   Billboard { get; }

    public RenderComponent(string modelId, float scale = 1f, bool billboard = false)
    {
        ModelId   = modelId ?? throw new ArgumentNullException(nameof(modelId));
        Scale     = scale;
        Billboard = billboard;
    }

    public EntityRenderInstance GetRenderInstance(System.Numerics.Vector3 position, float yawRadians)
        => new(ModelId, position, yawRadians);

    public void Update(IEntity entity, IModContext context, double deltaTime)
    {
        // Rendering erfolgt durch EntityRenderer – kein Update-Tick nötig.
    }

    public static RenderComponent FromJson(JsonElement config)
    {
        string modelId   = config.TryGetProperty("model",     out var mp) ? mp.GetString() ?? "" : "";
        float  scale     = config.TryGetProperty("scale",     out var sp) ? sp.GetSingle()       : 1f;
        bool   billboard = config.TryGetProperty("billboard", out var bp) && bp.GetBoolean();
        return new RenderComponent(modelId, scale, billboard);
    }
}
