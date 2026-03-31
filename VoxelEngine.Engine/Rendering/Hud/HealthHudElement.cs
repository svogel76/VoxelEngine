using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Rendering.Hud;

public sealed class HealthHudElement : IHudElement
{
    public string Id      => "health";
    public bool   Visible { get; set; } = true;

    private HudElementConfig _config = new(
        "health", HudAnchor.BottomLeft,
        OffsetX: 0.01f, OffsetY: 0.08f,
        Scale: 1f, Visible: true, ZOrder: 15);

    public HudElementConfig Config => _config;

    public float Health    { get; private set; }
    public float MaxHealth { get; private set; } = 20f;
    public float Ratio     => MaxHealth > 0f ? Math.Clamp(Health / MaxHealth, 0f, 1f) : 0f;
    public bool  IsCritical => Ratio < 0.2f;

    public void ApplyConfig(HudElementConfig config) => _config = config;

    public void Update(GameContext ctx)
    {
        var health = ctx.Player.GetComponent<HealthComponent>();
        Health    = health?.CurrentHp  ?? 20f;
        MaxHealth = health?.MaxHp      ?? 20f;
    }
}
