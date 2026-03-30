using Silk.NET.Input;

namespace VoxelEngine.Core.UI;

/// <summary>
/// Repräsentiert ein öffenbares UI-Fenster (Panel).
/// </summary>
public interface IUIPanel
{
    /// <summary>Eindeutige Kennung des Panels.</summary>
    string Id { get; }

    /// <summary>
    /// Taste, über die dieses Panel geöffnet/geschlossen wird.
    /// <c>null</c> bedeutet: kein direkter Tastenbinding (z.B. Spielmenü).
    /// </summary>
    Key? ToggleKey { get; }

    /// <summary>Wird aufgerufen wenn das Panel auf den Stack gepusht wird.</summary>
    void OnOpen(GameContext ctx);

    /// <summary>Wird aufgerufen wenn das Panel vom Stack entfernt wird.</summary>
    void OnClose(GameContext ctx);

    /// <summary>Wird jeden Frame aufgerufen solange das Panel offen ist.</summary>
    void Render(GameContext ctx, double frameTime);
}
