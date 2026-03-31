# VoxelEngine - Projektkontext

## Projektziel
Voxel-Engine im Minecraft-Stil mit Silk.NET und OpenGL in C# (.NET 10).
Architekturentscheidungen werden im Chat besprochen, Implementierung erfolgt in Claude Code.
Langfristiges Ziel: vollstaendiges Mod/Plugin-System - das Spiel selbst ist eine Mod.

## Projektstruktur (Zielzustand)
```
VoxelEngine.Engine/   # Class Library: Core/, Rendering/, World/, Entity/, Persistence/
VoxelEngine.Api/      # Class Library: oeffentliche Interfaces fuer Engine und Mods (kein Impl.)
VoxelEngine.Game/     # Mod-DLL: IGameMod-Implementierung, Bloecke, Assets
VoxelEngine.Launcher/ # Executable: laedt Engine + Mods, startet EngineRunner
VoxelEngine.Tests/    # xUnit + FluentAssertions
```

Aktueller Stand: VoxelEngine.Api ist als Vertrags-Assembly extrahiert.
VoxelEngine.Game ist noch Executable (wird kuenftig Mod-DLL), spricht Vertrags-Typen aber ueber VoxelEngine.Api.

## Architekturentscheidungen
- Fixed Timestep Game Loop (60 UPS, konfigurierbar via EngineSettings / engine.json)
- EngineSettings aus JSON: `Assets/engine.json`, Fallback auf C#-Defaults
- Keine Magic Numbers - alles via EngineSettings
- OpenGL only - kein Multi-Backend vorerst
- Shader als Embedded Resources in `VoxelEngine.Engine/Assets/Shaders/`
- `World/` hat keine Silk.NET-Abhaengigkeiten - pure C# fuer Portabilitaet
- GL-Aufrufe nur im Main Thread
- WorldTime ist zentrale Zeitvariable - alle zeitabhaengigen Systeme bauen darauf auf
- Multithreading: Chunk-Generierung + Mesh-Building im Background, GL-Uploads nur Main Thread
- `SampleBlock()` liefert deterministisches Meshing unabhaengig von Chunk-Ladereihenfolge
- Spieler-Entity trennt Position/Physik von der Kamera
- BlockRegistry ist die zentrale Quelle der Wahrheit fuer alle Block-Eigenschaften
- Block-Definitionen data-driven: `Assets/Blocks/*.json` + `Assets/Textures/blocks.manifest.json`
- Key Bindings data-driven: `Assets/keybindings.json`
- Dirty-Flag System: `Chunk.PlayerEdits` + `IsDirty`; `World.PersistedEdits` ueberlebt Unload/Reload
- HUD-Framework: `IHudElement` + `IHudRenderer`, konfigurierbar via `Assets/Hud/hud.json`
- Inventar: `Hotbar[9]`, `ItemStack`, `MaxStackSize` pro `BlockDefinition` (Default 64)
- Engine-Lifecycle: `EngineRunner` + `IGameMod`:
  `RegisterBlocks -> Initialize -> Update/Render -> Shutdown`
- Persistence: VXP5-Format (breaking change gegenueber VXP4 - keine Migration)

## Mod-System Architektur (naechster Meilenstein)
```
VoxelEngine.Api.dll          <- einzige Abhaengigkeit fuer Mods
    IGameMod                 <- Einstiegspunkt jeder Mod (inkl. VoxelGame)
    IModContext              <- Zugriff auf Engine-Systeme fuer Mods
    IComponent / IBehaviour  <- Bausteine fuer Entity-Logik
    IEntity, IGameContext    <- bestehende Interfaces sind hier extrahiert
    IBlockRegistry, IWorldAccess, IInputState, IKeyBindings

Mods/
    VoxelGame.dll            <- das Spiel als erste Mod
    MeineMod.dll             <- externe Mod, gleichberechtigt

mod.json pro Mod:
    { "id": "voxelgame", "name": "VoxelGame", "version": "1.0.0", "dependencies": [] }
```

## Naechste Schritte
- Schritt 2 des Backlogs: echtes Component System auf Basis von `VoxelEngine.Api.Entity.IComponent`
- Schritt 3: Behaviour Trees ueber `IBehaviour` und datengetriebene Entity-Definitionen
- Schritt 4: Mod-Loader + `mod.json`-Aufloesung, damit `VoxelEngine.Game` als DLL geladen wird

## Koordinaten-System
- Chunk-Koordinate: `Math.Floor(worldCoord / Chunk.Width)`
- Lokal-Koordinate: `((worldCoord % Width) + Width) % Width`
- Y hat keine Chunk-Unterteilung - Chunks gehen von Y=0 bis Y=255

## Vertex-Format
9 floats pro Vertex: `x, y, z, u, v, tileLayer, ao, faceLight, cutout`

## Physik-Konstanten (EngineSettings / engine.json)
- Gravity, MaxFallSpeed, JumpVelocity
- StepHeight (1.0f), EnableStepUp
- PlayerHeight (1.8f), PlayerWidth (0.6f), EyeHeight (1.62f)

## Neue Debug-Kommandos
Jedes Kommando als eigene Klasse in `Core/Debug/Commands/` - nie inline.
