using VoxelEngine.Core;
using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

/// <summary>
/// HUD-Element das den Gesundheitszustand des Spielers spiegelt.
/// Stellt Health/MaxHealth als normalisierte Ratio zur Verfügung (0..1)
/// sowie die gerundete Anzahl Herzen für eine Herzleisten-Anzeige.
/// </summary>
public sealed class HealthHudElement : IHudElement
{
    public string Id      => "health";
    public bool   Visible { get; set; } = true;

    private HudElementConfig _config = new(
        "health", HudAnchor.BottomLeft,
        OffsetX: 0.01f, OffsetY: 0.08f,
        Scale: 1f, Visible: true, ZOrder: 15);

    public HudElementConfig Config => _config;

    /// <summary>Aktueller Gesundheitswert (0..MaxHealth).</summary>
    public float Health    { get; private set; }
    /// <summary>Maximaler Gesundheitswert.</summary>
    public float MaxHealth { get; private set; } = 20f;
    /// <summary>Anteil Gesundheit als Ratio 0..1.</summary>
    public float Ratio     => MaxHealth > 0f ? Math.Clamp(Health / MaxHealth, 0f, 1f) : 0f;
    /// <summary>Gesundheit kritisch niedrig (&lt; 20 %).</summary>
    public bool  IsCritical => Ratio < 0.2f;

    public void ApplyConfig(HudElementConfig config) => _config = config;

    public void Update(GameContext ctx)
    {
        Health    = ctx.Player.Vitals.Health;
        MaxHealth = ctx.Player.Vitals.MaxHealth;
    }
}
