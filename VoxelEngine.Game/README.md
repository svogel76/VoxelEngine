# VoxelEngine.Game

> VoxelGame ist eine Mod — die erste und Referenz-Implementierung des Mod-Systems. Enthaelt Block-Definitionen, spielspezifische Assets und die `IGameMod`-Implementierung. Wird zur Laufzeit von `ModLoader` aus `Mods/VoxelGame/` geladen.

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
`-- VoxelGame.cs               # IGameMod-Implementierung
```

## mod.json

Jede Mod benoetigt eine `mod.json` im Ausgabe-Ordner (`Mods/VoxelGame/`):

```json
{
  "id": "voxelgame",
  "name": "VoxelGame",
  "version": "1.0.0",
  "dependencies": []
}
```

Felder: `id` (eindeutig, lowercase), `name` (Anzeigename), `version` (SemVer), `dependencies` (Liste von Mod-IDs).

## Assets

Assets liegen im Ausgabe-Ordner unter `Mods/VoxelGame/Assets/`. Der `IModContext` stellt `AssetBasePath` bereit — alle Asset-Pfade relativ dazu:

```csharp
var texturePath = Path.Combine(context.AssetBasePath, "Textures", "blocks.png");
```

## Ausfuehren

```bash
# Ueber den Launcher — VoxelEngine.Game NICHT direkt per dotnet run starten
dotnet run --project VoxelEngine.Launcher/VoxelEngine.Launcher.csproj

# Oder nach dem Build:
./Run/VoxelEngine.Launcher
```

Der Launcher baut VoxelGame automatisch als Abhaengigkeit (`ReferenceOutputAssembly=false`) und stellt sicher, dass `Mods/VoxelGame/VoxelGame.dll` immer aktuell ist.

## VoxelGame - Lifecycle-Uebersicht

```csharp
public class VoxelGame : IGameMod
{
    public void RegisterBlocks(IBlockRegistry registry)
    public void RegisterComponents(IComponentRegistry registry)
    public void RegisterBehaviours(IBehaviourRegistry registry)
    public void Initialize(IGameContext context)
    public void Update(double deltaTime)
    public void Render(double deltaTime)
    public void Shutdown()
}
```

## Eigene Komponenten registrieren

```csharp
public void RegisterComponents(IComponentRegistry registry)
{
    // Eigene IComponent-Implementierungen anmelden
    registry.Register("custom_drop", (entity, cfg) => new CustomDropComponent(entity, cfg));
}
```

## Eigene Behaviour-Tree-Nodes registrieren

```csharp
public void RegisterBehaviours(IBehaviourRegistry registry)
{
    registry.RegisterCondition("has_item", ctx => /* ... */);
    registry.RegisterAction("pick_up_item", (ctx, node) => new PickUpItemAction(ctx, node));
}
```

## Konventionen

- Block-IDs sind stabile Zahlen - nie umsortieren
- `MaxStackSize` pro Block setzen (Default 64 wenn weggelassen)
- Assets sind lose Dateien - kein Embedding (ContentCopyToOutputDirectory)
- Spielspezifische Logik gehoert ins Game-Projekt, nicht in die Engine
- `VoxelEngine.Api` ist die einzige erlaubte Abhaengigkeit auf Engine-Seite
