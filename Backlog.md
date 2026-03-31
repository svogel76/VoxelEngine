# VoxelEngine - Backlog

## Legende
- [Hoch] blockiert andere Features
- [Mittel] wichtig fuer Spielgefuehl
- [Nice-to-have] Qualitaet und Polish
- [Erledigt]

---

## Naechster Meilenstein — Mod/Plugin-System [HOECHSTE PRIORITAET]

> Ziel: Das Spiel ist eine Mod. Engine und Mods kennen sich nur ueber VoxelEngine.Api.
> Alles andere baut darauf auf — Content, Behaviour, Story, externe Mods.

### Schritt 1 — VoxelEngine.Api extrahieren [Hoch]
- Neues Class Library Projekt `VoxelEngine.Api` ohne Implementierung
- Bestehende Interfaces wandern hierher: `IGame` -> `IGameMod`, `IGameContext`,
  `IBlockRegistry`, `IWorldAccess`, `IInputState`
- Neue Interfaces: `IGameMod`, `IModContext`, `IEntity`, `IComponent`, `IBehaviour`
- Engine referenziert Api. Game referenziert Api. Beide kennen sich nicht direkt.

### Schritt 2 — Component System [Hoch]
- `IComponent` als Basisinterface in Api
- Konkrete Komponenten: `HealthComponent`, `PhysicsComponent`,
  `AIComponent`, `DropComponent`, `RenderComponent`
- Entity-Definition in JSON referenziert Komponenten per Name
- Ersetzt direkte Vererbungs-Hierarchie

### Schritt 3 — Behaviour Trees in JSON [Mittel]
- AI-Logik data-driven statt hardcodiertem Zustands-Automat
- Bausteine: Conditions (`player_near`, `health_low`),
  Actions (`flee`, `wander`, `idle`, `attack`)
- Komposition in Entity-JSON
- Neue Behaviours ohne Engine-Rebuild moeglich

### Schritt 4 — Mod-Loader + IGameMod [Hoch]
- Engine laedt DLLs zur Laufzeit aus `Mods/`-Ordner
- `mod.json` Manifest pro Mod (id, name, version, dependencies)
- `VoxelEngine.Game` wird zur ersten Mod-DLL
- Mod-Reihenfolge und Abhaengigkeiten werden aufgeloest

### Schritt 5 — Launcher extrahieren [Mittel]
- Neues Executable `VoxelEngine.Launcher`
- Laedt Engine, scannt Mods/, startet EngineRunner
- `VoxelEngine.Game` wird von Executable zu DLL
- Program.cs enthaelt nur noch Bootstrap-Code

---

## Phase 2.5 - Debug und Entwicklungswerkzeuge

- [Nice-to-have] Konsolen-Output farbig
- [Nice-to-have] Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
- `weather sunny/rain/snow/storm` -> Wetter (Phase 4)
- `season spring/summer/autumn/winter` -> Jahreszeit (Phase 4)
- `noise seed x` -> Seed aendern
- `entity list / spawn / kill` -> Entity-System (Phase 7)

---

## Phase 3 - Klimazonen und World Generation

### Klimazonen-Feintuning
- [Mittel] Polarregion + Tundra (sehr kalt, Permafrost, Eis)
- [Mittel] Mediterran (warm/trocken, Kalkstein)

### Hoehenzonen
- [Mittel] Kueste (Y=64-68, Sand/Kies Uebergang)
- [Mittel] Waldgrenze (pro Klimazone unterschiedlich)
- [Mittel] Schneegrenze (pro Klimazone unterschiedlich)
- [Mittel] Gipfelzone (reiner Stein/Schnee)

### Gewaesser - Generierung
- [Mittel] Seen (Mulden auffuellen)
- [Mittel] Gletscher (Polar/Tundra Klimazonen)
- [Nice-to-have] Fluesse (Pfad-Algorithmus von Bergen zum Meer)
- [Nice-to-have] Oasen (Wasser in Wuesten-Klimazone)

### Gewaesser - Simulation
- [Mittel] Level-System (1-8), Cellular Automata
- [Nice-to-have] Volumen-Erhaltung, Stroemung
- [Nice-to-have] Spieler-Interaktion (Block entfernen leitet Wasser um)

### Unterirdisches
- [Mittel] Hoehlen (3D Noise Density-Field)
- [Mittel] Erzvorkommen pro Klimazone
- [Nice-to-have] Dungeons, Katakomben, Verlorene Staedte

### Oberflaechen-Strukturen
- [Nice-to-have] Ruinen, Doerfer, Burgen pro Klimazone
- [Nice-to-have] Schablonen-Format (externe Datei)

---

## Phase 4 - Rendering und Visuals

- [Mittel] Jahreszeiten (Farb-Tint, Tageslaenge - DayCount vorhanden)
- [Mittel] Wetter-Zustandsautomat (Sonnig -> Bewoelkt -> Regen/Schnee)
- [Mittel] Regen/Schnee Partikel
- [Nice-to-have] Wolken (prozedural, ziehen mit Wind)
- [Nice-to-have] Sonnen-Halo / Atmosphaeren-Streuung
- [Nice-to-have] Distant Horizons (LOD-Silhouetten)
- [Nice-to-have] Billboard-Sprites fuer Vegetation (Gras, Blumen, Wippen im Wind)

---

## Phase 5 - Engine und Architektur [Erledigt]

- [Erledigt] Chunk-Serialisierung - `IWorldPersistence`, `LocalFilePersistence`,
  `InMemoryPersistence`, VXP5-Format
- [Erledigt] Projekttrennung in `VoxelEngine.Engine` (DLL) und `VoxelEngine.Game` (Exe)
- [Erledigt] `IGame`-Lifecycle eingefuehrt (`EngineRunner`, `IGame`, `IGameContext`)
- [Erledigt] Block-Definitionen data-driven (`Assets/Blocks/*.json`, `blocks.manifest.json`)
- [Erledigt] EngineSettings data-driven (`Assets/engine.json`)
- [Erledigt] Key Bindings data-driven (`Assets/keybindings.json`)
- [Nice-to-have] Asset-Management System
- [Nice-to-have] LOD (entfernte Chunks vereinfacht)

---

## Phase 6 - Gameplay und Simulation [Erledigt]

### UI und Menue
- [Erledigt] UI-Zustandsautomat - stack-basierter `UIStateManager`, `IUIPanel`, Escape-Logik
- [Erledigt] Spielmenue - Pause, Speichern, Beenden

### Inventar-System
- [Erledigt] Item-Icons in Hotbar (`IconRenderer`, batched Draw-Call)
- [Erledigt] Vollstaendiges Inventar-Fenster (4x9 Slots + Ausruestungs-Slots, Drag and Drop)
- [Nice-to-have] Crafting-System (Rezepte, Crafting-Tisch)
- [Nice-to-have] Werkzeug-Haltbarkeit

### Spieler-Erweiterungen
- [Erledigt] Gesundheits-System (HP, Schaden, Regeneration, Fallschaden)
- [Erledigt] Hunger-System (beeinflusst Regeneration, Verhungern)
- [Erledigt] Entity-Basisklasse (`Entity/`, Player erbt davon)
- [Nice-to-have] Erfahrungspunkte + Level
- [Nice-to-have] Spieler-Tod + Respawn

### Sound
- [Nice-to-have] Sound-System (OpenAL via Silk.NET)
- [Nice-to-have] Umgebungsgeraeusche pro Klimazone
- [Nice-to-have] Block-Sounds (abbauen, platzieren)
- [Nice-to-have] Spieler-Sounds (Schritte, Sprung, Landen)

---

## Phase 7 - Entity-System und Welt beleben

> Voraussetzung: Inventar-System [Erledigt]

### Entity-System Architektur
- [Erledigt] EntityManager (Frustum-Culling, Spatial Hashing)
- [Erledigt] Entity-Rendering (Voxel-Modelle + Entity-Atlas, batched)
- [Mittel] Entity-Kollision mit Terrain (AABB wie Spieler)
- [Erledigt] Entity-Spawning pro Klimazone
- [Nice-to-have] Entity-Persistenz
- [Nice-to-have] LOD fuer Entities

### Offene Gameplay-Items
- [Mittel] Block-Pickup (Abbauen -> Inventar)
- [Mittel] Dekrement beim Platzieren
- [Mittel] Fog-Command Inversion Bug

### Vegetation
- [Mittel] Gras + Blumen als Billboard-Sprites
- [Nice-to-have] Buesche, Pilze, Farne pro Klimazone

### Tiere
- [Mittel] Ressourcen droppen
- [Nice-to-have] Herden, Zucht

### NPCs und Feinde
- [Nice-to-have] NPC-Entity (Haendler, Dialog, Handel)
- [Nice-to-have] Feind-Entity (Aggro, Pathfinding, Kampf)
- [Nice-to-have] Dorf-Generierung

---

## Phase 8 - Multiplayer (optional, langfristig)

- [Nice-to-have] Option A: SpacetimeDB (C# Module, weniger Boilerplate, Free Tier)
- [Nice-to-have] Option B: ASP.NET Core + SignalR + MessagePack (volle Kontrolle)
- [Nice-to-have] Gemeinsam: Client-Side Prediction, Chunk-Streaming, Nametags, Chat

---

## Empfohlene Reihenfolge
```
Jetzt:       Mod/Plugin-System (Schritt 1-5 oben)
Dann:        Phase 7 - offene Gameplay-Items + Vegetation
Langfristig: Phase 8 - Multiplayer
```