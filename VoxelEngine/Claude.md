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
- Multithreading: Chunk-Generierung + Mesh-Building im Background, GL-Uploads nur Main Thread
- SampleBlock(): deterministisches Meshing unabhängig von Chunk-Ladereihenfolge

## Projektstruktur
VoxelEngine/
├── Assets/
│   ├── Fonts/
│   │   └── font.png
│   └── Shaders/
│       ├── basic.vert            # MVP + UV + TileLayer + AO + FaceLight
│       ├── basic.frag            # Beleuchtung + Fog + Transparenz
│       ├── text.vert             # Orthografische 2D Projektion
│       ├── text.frag             # Font-Rendering mit discard
│       ├── skybox.vert           # Far Plane (pos.xyww)
│       ├── skybox.frag           # Gradient Zenith/Horizont/Boden
│       ├── celestial.vert        # Billboard Far Plane
│       ├── celestial.frag        # Sonne/Mond Textur + Opacity
│       └── stars.vert/frag       # Instanced Billboards + Twinkle
├── Core/
│   ├── Debug/
│   │   ├── Commands/
│   │   │   ├── ChunkInfoCommand.cs
│   │   │   ├── FogCommand.cs
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
│   ├── ArrayTexture.cs           # Texture2DArray, 11 Schichten (inkl. Water/Glass/Ice)
│   ├── BitmapFont.cs
│   ├── Camera.cs
│   ├── CelestialBody.cs          # Billboard Quad für Sonne/Mond
│   ├── CelestialTextures.cs      # Sonne + Mondphasen programmatisch
│   ├── ChunkRenderer.cs          # Two-Pass, UploadPendingMeshes (max 4/Frame)
│   ├── DebugOverlay.cs
│   ├── FrustumCuller.cs
│   ├── GreedyMeshBuilder.cs      # 3-Achsen-Sweep, NeedsFace, SampleBlock
│   ├── Mesh.cs                   # VAO/VBO/EBO, Stride 8 floats
│   ├── Renderer.cs
│   ├── Shader.cs
│   ├── Skybox.cs                 # Gradient + Sonne/Mond + Sterne
│   ├── SkyColorCurve.cs          # 8 Keyframes, AmbientLight, SunColor
│   ├── StarField.cs              # 1500 Sterne, Instanced Rendering
│   ├── TextRenderer.cs
│   └── Texture.cs
└── World/
    ├── BlockTextures.cs          # Tile-Index pro BlockType + FaceDirection
    ├── BlockType.cs              # Air/Grass/Dirt/Stone/Sand/Water/Glass/Ice
    ├── Chunk.cs
    ├── ChunkJob.cs               # readonly record struct
    ├── ChunkManager.cs           # Update, EnqueueJob, TryDequeueResult
    ├── ChunkResult.cs            # Fertige Mesh-Daten für GPU-Upload
    ├── ChunkWorker.cs            # Background ThreadPool, ConcurrentQueues
    ├── FaceDirection.cs
    ├── NoiseSettings.cs
    ├── World.cs                  # ConcurrentDictionary, AddChunk, SampleBlock
    ├── WorldGenerator.cs         # GenerateChunk() gibt Chunk zurück, SeaLevel=64
    └── WorldTime.cs              # Time, DayCount, MoonPhase, TimeScale

## Koordinaten-System
- Chunk-Koordinate:  Math.Floor(worldCoord / Chunk.Width)
- Lokal-Koordinate:  ((worldCoord % Width) + Width) % Width
- Y hat keine Chunk-Unterteilung — Chunks gehen von Y=0 bis Y=255

## Vertex-Format
8 floats pro Vertex: x, y, z, u, v, tileLayer, ao, faceLight

## Aktueller Stand
- [x] Engine-Grundstruktur mit Fixed Timestep Game Loop
- [x] EngineSettings als zentrale Konfiguration
- [x] Kamera mit Maus/Tastatur (WASD + Space/Shift, InvertMouseY)
- [x] InputHandler mit Raw Mouse Input
- [x] Shader-System (Shader.cs mit Fehlerprüfung)
- [x] Mesh-System (VAO/VBO/EBO, Stride 8 floats)
- [x] Texture-System (StbImageSharp)
- [x] FPS-Anzeige im Fenstertitel + HUD
- [x] MVP-Matrix Pipeline
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Perlin Noise Terrain-Generation mit NoiseSettings
- [x] Greedy Meshing (3-Achsen-Sweep, NeedsFace, AO-korrekter Merge)
- [x] ArrayTexture (Texture2DArray, 11 Schichten)
- [x] Two-Pass Rendering (opaque + transparent, DepthMask)
- [x] Transparente Blöcke (Water=5, Glass=6, Ice=7)
- [x] Meeresspiegel Y=64
- [x] SampleBlock() — deterministisches Meshing
- [x] NeedsFace() — korrekte Flächen-Logik für alle Block-Kombinationen
- [x] Backface Culling (CCW Winding Order)
- [x] GameContext (zentraler Container)
- [x] Bitmap Font System (CP437)
- [x] Debug-Konsole (F1, ICommand Interface)
- [x] HUD (FPS, Pos, Chunks, Verts, Time)
- [x] Kommandos: help, pos, tp, wireframe, chunk info, renderdistance, skybox, time, fog
- [x] Chunk-Manager (dynamisches Laden/Entladen, Hysterese)
- [x] Multithreading (ChunkWorker, ConcurrentQueues, GL-Upload Main Thread)
- [x] Frustum Culling (Gribb-Hartmann, AABB-Test)
- [x] Ambient Occlusion (VertexAO, Diagonal-Flip)
- [x] Skybox (prozeduraler Gradient)
- [x] WorldTime (Time, DayCount, MoonPhase, TimeScale)
- [x] SkyColorCurve (8 Keyframes, SunColor, AmbientLight)
- [x] Sonne + Mond (Billboard Quads, Mondphasen)
- [x] Sterne (Instanced Rendering, Twinkle)
- [x] Diffuse Beleuchtung (FaceLight, uGlobalLight, uSunColor)
- [x] Fog (linearer Nebel, FogColor aus Skybox, Tag/Nacht)
- [ ] Konsolen-History + Autocomplete
- [ ] Block-Interaktion (Blöcke setzen/abbauen)
- [ ] Spieler-Entity (trennt Kamera von Spieler)
- [ ] Phase 3 — Klimazonen

## Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers — alles über EngineSettings
- Unsafe-Blöcke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren
- GL-Aufrufe NUR im Main Thread (niemals im ChunkWorker)
- Skybox vor Terrain rendern — DepthTest nach Skybox immer reaktivieren