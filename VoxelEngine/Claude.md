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
- WorldTime ist zentrale Zeitvariable — alle Zeit-abhängigen Systeme bauen darauf auf

## Projektstruktur
VoxelEngine/
├── Assets/
│   ├── Fonts/
│   │   └── font.png
│   └── Shaders/
│       ├── basic.vert
│       ├── basic.frag
│       ├── text.vert
│       ├── text.frag
│       ├── skybox.vert
│       ├── skybox.frag
│       ├── celestial.vert
│       └── celestial.frag
├── Core/
│   ├── Debug/
│   │   ├── Commands/
│   │   │   ├── ChunkInfoCommand.cs
│   │   │   ├── HelpCommand.cs
│   │   │   ├── PosCommand.cs
│   │   │   ├── RenderDistanceCommand.cs
│   │   │   ├── SkyboxCommand.cs
│   │   │   ├── TeleportCommand.cs
│   │   │   ├── TimeCommand.cs
│   │   │   └── WireframeCommand.cs
│   │   ├── DebugConsole.cs
│   │   └── ICommand.cs
│   ├── Engine.cs
│   ├── EngineSettings.cs
│   ├── GameContext.cs
│   ├── GameLoop.cs
│   └── InputHandler.cs
├── Rendering/
│   ├── ArrayTexture.cs           # Texture2DArray, 8 Schichten
│   ├── BitmapFont.cs
│   ├── Camera.cs
│   ├── CelestialBody.cs          # Billboard Quad für Sonne/Mond
│   ├── CelestialTextures.cs      # Sonne + Mondphasen programmatisch
│   ├── ChunkRenderer.cs
│   ├── DebugOverlay.cs           # HUD: FPS, Pos, Chunks, Verts, Time
│   ├── FrustumCuller.cs
│   ├── GreedyMeshBuilder.cs      # 3-Achsen-Sweep, AO-korrekter Merge
│   ├── Mesh.cs                   # VAO/VBO/EBO, Stride 7 floats
│   ├── Renderer.cs
│   ├── Shader.cs
│   ├── Skybox.cs                 # Prozeduraler Himmel + Sonne/Mond
│   ├── SkyColorCurve.cs          # Keyframe-Interpolation für Tagesfarben
│   ├── TextRenderer.cs
│   └── Texture.cs
└── World/
    ├── BlockTextures.cs
    ├── BlockType.cs
    ├── Chunk.cs
    ├── ChunkManager.cs
    ├── FaceDirection.cs
    ├── NoiseSettings.cs
    ├── World.cs
    ├── WorldGenerator.cs
    └── WorldTime.cs              # Time, DayCount, MoonPhase, TimeScale

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
- [x] Mesh-System (VAO/VBO/EBO, Stride 7 floats)
- [x] Texture-System (StbImageSharp + CreateFromBytes Fallback)
- [x] FPS-Anzeige im Fenstertitel
- [x] MVP-Matrix Pipeline (Model/View/Projection als Uniforms)
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Perlin Noise Terrain-Generation mit NoiseSettings
- [x] Greedy Meshing (3-Achsen-Sweep, AO-korrekter Merge, UV-Kachelung)
- [x] ArrayTexture (Texture2DArray, 8 Schichten, Nearest-Filtering)
- [x] ChunkRenderer — Welt wird gerendert
- [x] Backface Culling (CCW Winding Order)
- [x] GameContext (zentraler Container für alle Systeme)
- [x] Bitmap Font System (CP437, Orthografische Projektion)
- [x] Debug-Konsole (F1, Command-Registry, ICommand Interface)
- [x] HUD (FPS, Position, Chunks X/Y, Verts, Time)
- [x] Kommandos: help, pos, tp, wireframe, chunk info, renderdistance, skybox, time
- [x] Chunk-Manager (dynamisches Laden/Entladen, Hysterese)
- [x] Frustum Culling (FrustumCuller, Gribb-Hartmann, AABB-Test)
- [x] Ambient Occlusion (VertexAO, Diagonal-Flip, Z-Fighting Fix)
- [x] Skybox (prozeduraler Himmel, Gradient Zenith/Horizont/Boden)
- [x] WorldTime (Time, DayCount, MoonPhase, TimeScale, Paused)
- [x] SkyColorCurve (Keyframe-Interpolation, 7 Tagesphasen)
- [x] Sonne (Billboard Quad, Winkel aus WorldTime, Opacity-Fade)
- [x] Mond (Billboard Quad, Mondphasen 0-7, Helligkeit bei Vollmond)
- [x] Fog (linearer Entfernungs-Nebel, FogColor aus Skybox, Tag/Nacht-Anpassung)
- [x] FogCommand (fog on/off, fog start/end)
- [ ] Konsolen-History + Autocomplete
- [ ] Transparente Blöcke (Wasser)
- [ ] Diffuse Beleuchtung (Sonnenrichtung als Shader-Uniform)
- [ ] Licht/Schatten System

## Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers — alles über EngineSettings
- Unsafe-Blöcke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren
- Skybox vor Terrain rendern — Reihenfolge kritisch
- DepthTest nach Skybox-Render immer wieder aktivieren