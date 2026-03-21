# VoxelEngine — Projektkontext für Claude Code

## Projektziel
Voxel-Engine im Minecraft-Stil mit Silk.NET und OpenGL in C# (.NET 8).
Lernprojekt: Architekturentscheidungen werden im Chat besprochen,
Implementierung erfolgt in Claude Code.

## Architekturentscheidungen
- Fixed Timestep Game Loop (60 UPS, konfigurierbar via EngineSettings)
- EngineSettings als zentrale Konfigurationsklasse (init-Properties, keine Magic Numbers)
- OpenGL only — kein Multi-Backend vorerst
- Shader als externe .glsl Dateien unter Assets/Shaders/
- Kamera mit Yaw/Pitch, InvertMouseY-Option in EngineSettings
- Keine Magic Numbers — alles über EngineSettings konfigurierbar
- World/ hat keine Silk.NET Abhängigkeiten — pure C# für Portabilität

## Projektstruktur
VoxelEngine/
├── Assets/
│   ├── Fonts/
│   │   └── font.png              # CP437 Bitmap Font (16×16 ASCII Grid)
│   └── Shaders/
│       ├── basic.vert            # MVP-Transformation + UV-Koordinaten
│       ├── basic.frag            # Textur-Sampling
│       ├── text.vert             # Orthografische 2D Projektion
│       └── text.frag             # Font-Rendering mit discard
├── Core/
│   ├── Debug/
│   │   ├── Commands/
│   │   │   ├── ChunkInfoCommand.cs
│   │   │   ├── HelpCommand.cs
│   │   │   ├── PosCommand.cs
│   │   │   ├── TeleportCommand.cs
│   │   │   └── WireframeCommand.cs
│   │   ├── DebugConsole.cs       # Command-Registry, History, Output-Log
│   │   └── ICommand.cs           # Interface: Name, Description, Usage, Execute
│   ├── Engine.cs                 # Hauptklasse, Silk.NET Window + Loop
│   ├── EngineSettings.cs         # Zentrale Konfiguration
│   ├── GameContext.cs            # Container für alle Systeme
│   ├── GameLoop.cs
│   └── InputHandler.cs
├── Rendering/
│   ├── BitmapFont.cs             # Font-Atlas Textur, UV-Berechnung pro Zeichen
│   ├── Camera.cs
│   ├── ChunkMeshBuilder.cs
│   ├── ChunkRenderer.cs
│   ├── DebugOverlay.cs           # HUD + Konsolen-Overlay
│   ├── Mesh.cs
│   ├── Renderer.cs
│   ├── Shader.cs
│   ├── TextRenderer.cs           # DynamicDraw VBO, DrawArrays, 2D Quads
│   └── Texture.cs
└── World/
    ├── BlockType.cs
    ├── Chunk.cs
    ├── NoiseSettings.cs
    ├── World.cs
    └── WorldGenerator.cs

## Koordinaten-System
- Chunk-Koordinate:  Math.Floor(worldCoord / Chunk.Width)
- Lokal-Koordinate:  ((worldCoord % Width) + Width) % Width
- Y hat keine Chunk-Unterteilung — Chunks gehen von Y=0 bis Y=255

## Aktueller Stand
- [x] Engine-Grundstruktur mit Fixed Timestep Game Loop
- [x] EngineSettings als zentrale Konfiguration
- [x] Kamera mit Maus/Tastatur (WASD + Space/Shift, InvertMouseY)
- [x] InputHandler mit Raw Mouse Input
- [x] Shader-System (Shader.cs mit Fehlerprüfung)
- [x] Mesh-System (VAO/VBO/EBO)
- [x] Texture-System (StbImageSharp + CreateFromBytes Fallback)
- [x] FPS-Anzeige im Fenstertitel
- [x] MVP-Matrix Pipeline (Model/View/Projection als Uniforms)
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Perlin Noise Terrain-Generation mit NoiseSettings
- [x] Naive Culling Meshing (ChunkMeshBuilder)
- [x] ChunkRenderer — Welt wird gerendert
- [x] Backface Culling (CCW Winding Order, alle 6 Seiten verifiziert)
- [x] GameContext (zentraler Container für alle Systeme)
- [x] Bitmap Font System (CP437, UV-Berechnung, Orthografische Projektion)
- [x] Debug-Konsole (F1, Command-Registry, ICommand Interface)
- [x] HUD (FPS + Position, immer sichtbar)
- [x] Kommandos: help, pos, tp, wireframe, chunk info
- [ ] Chunk-Manager (dynamisches Laden/Entladen)
- [ ] Textur-Atlas (verschiedene Texturen pro Block-Typ)
- [ ] Perlin Noise Terrain-Generation mit NoiseSettings

## Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers — alles über EngineSettings
- Unsafe-Blöcke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren

## Nächste Schritte (Phase 2 Fortsetzung)
1. Perlin Noise Höhenkarte — erste echte Terrain-Generation
2. Chunk-Manager — dynamisches Laden um Spielerposition
3. Textur-Atlas — verschiedene Texturen pro Block-Typ
4. Backface Culling — GPU rendert Rückseiten nicht