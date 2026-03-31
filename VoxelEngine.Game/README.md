# VoxelEngine.Game

> Das konkrete Spiel auf Basis von `VoxelEngine.Engine`. Enthaelt Block-Definitionen, spielspezifische Assets und die `IGameMod`-Implementierung. Die gemeinsamen Vertrags-Typen kommen aus `VoxelEngine.Api`.

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
|-- VoxelGame.cs               # IGameMod-Implementierung
`-- Program.cs                 # Temporärer Einstiegspunkt bis Launcher-Extraktion
```

## Einstiegspunkt

`VoxelGame` implementiert `IGameMod` und verdrahtet alle spielspezifischen Systeme mit der Engine. Die Vertrags-Typen fuer `RegisterBlocks` und `Initialize` kommen aus `VoxelEngine.Api`.

## VoxelGame - Lifecycle-Uebersicht

```csharp
public class VoxelGame : IGameMod
{
    public void RegisterBlocks(IBlockRegistry registry)
    public void Initialize(IGameContext context)
    public void Update(double deltaTime)
    public void Render(double deltaTime)
    public void Shutdown()
}
```

## Konventionen

- Block-IDs sind stabile Zahlen - nie umsortieren
- `MaxStackSize` pro Block setzen (Default 64 wenn weggelassen)
- Assets sind lose Dateien - kein Embedding
- Spielspezifische Logik gehoert ins Game-Projekt, nicht in die Engine
