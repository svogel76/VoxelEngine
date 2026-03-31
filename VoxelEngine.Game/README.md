# VoxelEngine.Game

> Das konkrete Spiel auf Basis von `VoxelEngine.Engine`. Enthaelt Block-Definitionen, spielspezifische Assets und die `IGame`-Implementierung. Dieses Dokument erklaert, wie das Spiel erweitert wird.

## Projektstruktur

```
VoxelEngine.Game/
|-- Assets/
|   |-- Blocks/                # Eine JSON-Datei pro BlockDefinition
|   |-- Hud/
|   |   `-- hud.json           # HUD-Layout und Element-Konfiguration
|   |-- Fonts/                 # Bitmap-Fonts
|   `-- Textures/              # Block-/Entity-Texturen und blocks.manifest.json
|-- Blocks/
|   `-- BlockDefinitionLoader.cs # Laedt BlockDefinitions aus Assets/Blocks
|-- VoxelGame.cs               # IGame-Implementierung
`-- Program.cs                 # Einstiegspunkt
```

## Einstiegspunkt

```csharp
// Program.cs
new EngineRunner().Run(new VoxelGame());
```

`VoxelGame` implementiert `IGame` und verdrahtet alle spielspezifischen Systeme mit der Engine.

## Einen neuen Block hinzufuegen

Alle Bloecke werden als JSON-Dateien in `Assets/Blocks/` definiert und von `Blocks/BlockDefinitionLoader.cs` geladen.

**Schritt 1** - Block-JSON anlegen:

```json
{
  "id": 42,
  "name": "sandstone",
  "textures": {
    "top": "sandstone",
    "side": "sandstone",
    "bottom": "sandstone"
  },
  "properties": {
    "solid": true,
    "transparent": false,
    "replaceable": false,
    "max_stack": 64
  },
  "behaviour": "default"
}
```

**Schritt 2** - Textur hinzufuegen:
Textur-Namen werden ueber `Assets/Textures/blocks.manifest.json` auf Layer-Indizes gemappt. Neuen Namen in das Manifest eintragen und dann in der Block-JSON referenzieren.

**Schritt 3** - Kompilieren und testen. Kein weiterer Boilerplate noetig - Meshing, Kollision und Serialisierung greifen automatisch.

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

## VoxelGame - Lifecycle-Uebersicht

```csharp
public class VoxelGame : IGame
{
    public void RegisterBlocks(IBlockRegistry registry)
        // <- BlockDefinitionLoader aufrufen

    public void Initialize(IGameContext context)
        // <- Spieler, Welt, UI initialisieren

    public void Update(double deltaTime)
        // <- Spiellogik (Input, Physik, Inventar ...)

    public void Render(double deltaTime)
        // <- Spielseitiges Rendering (HUD, UI-Overlays ...)

    public void Shutdown()
        // <- Persistenz flushen, Ressourcen freigeben
}
```

## Klimazonen & Spawn-Konfiguration

Klimazonen und Entity-Spawns werden ueber JSON-Dateien in `Assets/Climate/` definiert - nicht in C#-Code. Neue Klimazone anlegen:

1. JSON-Datei in `Assets/Climate/` erstellen (Schema: bestehende Dateien als Vorlage)
2. Spawn-Eintraege direkt in der Klimazonen-JSON pflegen (nicht in Entity-Definitionen)
3. Kein Code-Aenderung noetig - das System laedt alle JSONs automatisch

## Konventionen

- Block-IDs sind stabile Zahlen - nie umsortieren (Persistenz-Format speichert IDs)
- `MaxStackSize` pro Block setzen (Default 64 wenn weggelassen)
- Assets sind lose Dateien - kein Embedding, damit sie zur Laufzeit editierbar bleiben
- Spielspezifische Logik gehoert in `VoxelGame.cs` oder eigene Klassen im Game-Projekt - nie in die Engine
