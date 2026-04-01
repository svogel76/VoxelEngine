# VoxelEngine - Projektkontext

## Projektziel
Voxel-Engine im Minecraft-Stil mit Silk.NET und OpenGL in C# (.NET 10).
Architekturentscheidungen werden im Chat besprochen, Implementierung erfolgt in Claude Code.
Das Spiel selbst ist eine Mod - die Engine kennt kein spezifisches Spiel.

## Projektstruktur
```
VoxelEngine.Api/      # Interfaces only — keine Implementierung
VoxelEngine.Engine/   # Core, Rendering, World, Entity, Persistence
VoxelEngine.Game/     # Mod-DLL: IGameMod, Bloecke, Assets → Mods/VoxelGame/
VoxelEngine.Launcher/ # Executable: 2-Zeilen Bootstrap (ModLoader + EngineRunner)
VoxelEngine.Tests/    # xUnit + FluentAssertions
Mods/VoxelGame/       # Laufzeit-Ausgabe: VoxelGame.dll + Assets
Run/                  # Startverzeichnis (gitignored)
```

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
  `RegisterBlocks -> RegisterComponents -> RegisterBehaviours -> Initialize -> Update/Render -> Shutdown`
- Persistence: VXP5-Format (breaking change gegenueber VXP4 - keine Migration)
- Alle Entities sind plain `Entity`-Instanzen, zusammengesetzt aus `IComponent`-Implementierungen
- Behaviour Trees sind datengetrieben via JSON (`IBehaviourNode`, `BehaviourRegistry`);
  Conditions und Actions werden per Name aus der Registry aufgeloest
- Mod-Loader scannt `Mods/` zur Laufzeit, laedt Assemblies in dieselbe AppDomain,
  liest `mod.json` und loest Abhaengigkeiten auf
- Jede Mod erhaelt einen eigenen `IModContext` mit `AssetBasePath` als Wurzel fuer Assets
- `VoxelEngine.Game` ist eine Mod - keine Sonder-Privilegien gegenueber externen Mods
- Der Launcher referenziert nur `VoxelEngine.Engine` - keinerlei Spiellogik

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
