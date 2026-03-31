# VoxelEngine.Game

> Das konkrete Spiel auf Basis von `VoxelEngine.Engine`. Enthält Block-Definitionen, spielspezifische Assets und die `IGame`-Implementierung. Dieses Dokument erklärt, wie das Spiel erweitert wird.

## Projektstruktur

```
VoxelEngine.Game/
├── Assets/
│   ├── Hud/
│   │   └── hud.json          # HUD-Layout und Element-Konfiguration
│   ├── Fonts/                # Bitmap-Fonts
│   └── Textures/             # Block- und Entity-Texturen (ArrayTexture)
├── Blocks/
│   └── BlockRegistration.cs  # Alle BlockDefinitions — zentrale Anlaufstelle
├── VoxelGame.cs              # IGame-Implementierung
└── Program.cs                # Einstiegspunkt
```

## Einstiegspunkt

```csharp
// Program.cs
new EngineRunner().Run(new VoxelGame());
```

`VoxelGame` implementiert `IGame` und verdrahtet alle spielspezifischen Systeme mit der Engine.

## Einen neuen Block hinzufügen

Alle Blöcke werden in `Blocks/BlockRegistration.cs` registriert — nirgendwo sonst.

**Schritt 1** — BlockDefinition anlegen:

```csharp
registry.Register(new BlockDefinition
{
    Id           = 42,
    Name         = "Sandstone",
    TextureTop   = 15,   // Layer-Index im Texture2DArray
    TextureSide  = 15,
    TextureBottom= 16,
    MaxStackSize = 64,
    IsSolid      = true,
    IsTransparent= false,
});
```

**Schritt 2** — Textur hinzufügen:
Die Textur-Layer entsprechen der Reihenfolge in `Assets/Textures/blocks.png` (ArrayTexture-Aufbau). Neuen Layer ans Ende anhängen und den Index in der BlockDefinition setzen.

**Schritt 3** — Kompilieren und testen. Kein weiterer Boilerplate nötig — Meshing, Kollision und Serialisierung greifen automatisch.

## Assets

### hud.json

Konfiguriert alle HUD-Elemente deklarativ:

```json
{
  "elements": [
    { "type": "Hotbar",   "anchor": "BottomCenter" },
    { "type": "Crosshair","anchor": "Center"       },
    { "type": "FpsCounter","anchor": "TopLeft",  "visible": true }
  ]
}
```

Neue HUD-Elemente implementieren `IHudElement` + `IHudRenderer` in der Engine und werden hier per `"type"` referenziert.

### Texturen

| Datei | Inhalt |
|---|---|
| `Textures/blocks.png` | Block-Textur-Atlas (Texture2DArray) |
| `Textures/entities.png` | Entity-Sprite-Atlas |
| `Fonts/font.png` | CP437-Bitmap-Font |

## VoxelGame — Lifecycle-Übersicht

```csharp
public class VoxelGame : IGame
{
    public void RegisterBlocks(IBlockRegistry registry)
        // ← BlockRegistration.cs aufrufen

    public void Initialize(IGameContext context)
        // ← Spieler, Welt, UI initialisieren

    public void Update(double deltaTime)
        // ← Spiellogik (Input, Physik, Inventar …)

    public void Render(double deltaTime)
        // ← Spielseitiges Rendering (HUD, UI-Overlays …)

    public void Shutdown()
        // ← Persistenz flushen, Ressourcen freigeben
}
```

## Klimazonen & Spawn-Konfiguration

Klimazonen und Entity-Spawns werden über JSON-Dateien in `Assets/Climate/` definiert — nicht in C#-Code. Neue Klimazone anlegen:

1. JSON-Datei in `Assets/Climate/` erstellen (Schema: bestehende Dateien als Vorlage)
2. Spawn-Einträge direkt in der Klimazonen-JSON pflegen (nicht in Entity-Definitionen)
3. Kein Code-Änderung nötig — das System lädt alle JSONs automatisch

## Konventionen

- Block-IDs sind stabile Zahlen — nie umsortieren (Persistenz-Format speichert IDs)
- `MaxStackSize` pro Block setzen (Default 64 wenn weggelassen)
- Assets sind lose Dateien — kein Embedding, damit sie zur Laufzeit editierbar bleiben
- Spielspezifische Logik gehört in `VoxelGame.cs` oder eigene Klassen im Game-Projekt — nie in die Engine
