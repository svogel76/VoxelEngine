# VoxelEngine - Projektkontext

## Projektziel
Voxel-Engine im Minecraft-Stil mit Silk.NET und OpenGL in C# (.NET 10).
Lernprojekt: Architekturentscheidungen werden im Chat besprochen,
Implementierung erfolgt in Claude Code.

## Projektstruktur
```
VoxelEngine/
|- Core/           # Engine-Infrastruktur, Game Loop, Input, Debug-Konsole, HUD-Registry
|- Rendering/      # OpenGL + Silk.NET - Shader, Mesh, Chunk-Renderer, HUD-Renderer
|- World/          # Pure C# - KEIN Silk.NET - Chunks, Physik, Inventar, Registries
`- Assets/         # Externe Ressourcen: Shader (.glsl), Fonts, hud.json
VoxelEngine.Tests/  # xUnit + FluentAssertions - spiegelt World/ + Core/
```

## Architekturentscheidungen
- Fixed Timestep Game Loop (60 UPS, konfigurierbar via EngineSettings)
- EngineSettings als zentrale Konfigurationsklasse (init-properties, keine Magic Numbers)
- OpenGL only - kein Multi-Backend vorerst
- Shader als externe `.glsl` Dateien unter `Assets/Shaders/`
- `World/` hat keine Silk.NET-Abhaengigkeiten - pure C# fuer Portabilitaet
- WorldTime ist zentrale Zeitvariable - alle zeitabhaengigen Systeme bauen darauf auf
- Multithreading: Chunk-Generierung + Mesh-Building im Background, GL-Uploads nur Main Thread
- `SampleBlock()` liefert deterministisches Meshing unabhaengig von Chunk-Ladereihenfolge
- Spieler-Entity trennt Position/Physik von der Kamera
- BlockRegistry ist die zentrale Quelle der Wahrheit fuer alle Block-Eigenschaften
- Dirty-Flag System: `Chunk.PlayerEdits` + `IsDirty`; `World.PersistedEdits` ueberlebt Unload/Reload
- HUD-Framework: `IHudElement` + `IHudRenderer`, konfigurierbar via `Assets/Hud/hud.json`
- Inventar: `Hotbar[9]`, `ItemStack`, `MaxStackSize` pro `BlockDefinition` (Default 64)

## Koordinaten-System
- Chunk-Koordinate: `Math.Floor(worldCoord / Chunk.Width)`
- Lokal-Koordinate: `((worldCoord % Width) + Width) % Width`
- Y hat keine Chunk-Unterteilung - Chunks gehen von `Y=0` bis `Y=255`

## Vertex-Format
9 floats pro Vertex: `x, y, z, u, v, tileLayer, ao, faceLight, cutout`

## Physik-Konstanten (EngineSettings)
- Gravity, MaxFallSpeed, JumpVelocity
- StepHeight (1.0f), EnableStepUp
- PlayerHeight (1.8f), PlayerWidth (0.6f), EyeHeight (1.62f)

## Naechste Schritte
- Spawn-Balancing pro Klimazone verfeinern (weitere Tiere, Aktivitaetsmuster, Spawn-Orte)
- Entity-Persistenz fuer klimaabhaengig gespawnte Tiere vorbereiten
