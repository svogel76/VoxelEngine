namespace VoxelEngine.Core.Hud;

/// <summary>Unveränderliche Konfiguration eines HUD-Elements (aus hud.json).</summary>
public record HudElementConfig(
    string    Id,
    HudAnchor Anchor,
    float     OffsetX,   // relativ zur Bildschirmbreite (0.0–1.0)
    float     OffsetY,   // relativ zur Bildschirmhöhe  (0.0–1.0)
    float     Scale,
    bool      Visible,
    int       ZOrder
);
