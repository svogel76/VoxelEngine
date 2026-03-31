using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Rendering.Hud;

/// <summary>
/// HUD-Element das den Hungerzustand des Spielers spiegelt.
/// Analog zu <see cref="HealthHudElement"/> — Hunger als Ratio 0..1
/// plus Schwellwert-Flags für die Renderer-Logik.
/// </summary>
public sealed class HungerHudElement : IHudElement
{
    public string Id      => "hunger";
    public bool   Visible { get; set; } = true;

    private HudElementConfig _config = new(
        "hunger", HudAnchor.BottomRight,
        OffsetX: -0.01f, OffsetY: 0.08f,
        Scale: 1f, Visible: true, ZOrder: 16);

    public HudElementConfig Config => _config;

    /// <summary>Aktueller Hungerwert (0..MaxHunger).</summary>
    public float Hunger    { get; private set; }
    /// <summary>Maximaler Hungerwert.</summary>
    public float MaxHunger { get; private set; } = 20f;
    /// <summary>Anteil Hunger als Ratio 0..1.</summary>
    public float Ratio     => MaxHunger > 0f ? Math.Clamp(Hunger / MaxHunger, 0f, 1f) : 0f;
    /// <summary>Spieler verhungert (Hunger = 0 — Schaden aktiv).</summary>
    public bool  IsStarving  => Hunger <= 0f;
    /// <summary>Regeneration aktiv (Hunger ≥ RegenSchwelle). Nur visueller Hinweis.</summary>
    public bool  IsRegenerating { get; private set; }

    public void ApplyConfig(HudElementConfig config) => _config = config;

    public void Update(GameContext ctx)
    {
        // Hunger ist nicht implementiert — immer voll
        Hunger    = MaxHunger;

        // Regeneration ist aktiv wenn Hunger hoch UND Health nicht voll
        var health = ctx.Player.GetComponent<HealthComponent>();
        IsRegenerating = Ratio >= 0.9f && (health is null || health.CurrentHp < health.MaxHp);
    }
}
