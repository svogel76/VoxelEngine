# VoxelEngine - Backlog

## Legende
- [Hoch] blockiert andere Features
- [Mittel] wichtig fuer Spielgefuehl
- [Nice-to-have] Qualitaet und Polish
- [Erledigt]

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

- [Erledigt] Chunk-Serialisierung - `IWorldPersistence`, `LocalFilePersistence` (Region-Dateien `.vxr`), `InMemoryPersistence`, `SaveDirectory` in `EngineSettings`
- [Nice-to-have] Asset-Management System
- [Nice-to-have] LOD (entfernte Chunks vereinfacht)

---

## Phase 6 - Gameplay und Simulation [Erledigt]

### UI und Menue
- [Erledigt] UI-Zustandsautomat - stack-basierter `UIStateManager`, `IUIPanel`, Escape-Logik
- [Erledigt] Spielmenue - Pause, Speichern, Beenden

### Inventar-System
- [Erledigt] Item-Icons in Hotbar (Top-Face aus ArrayTexture, `IconRenderer`, batched Draw-Call)
- [Erledigt] Vollstaendiges Inventar-Fenster (4x9 Slots + Ausruestungs-Slots, Drag and Drop, Shift-Click)
- [Nice-to-have] Crafting-System (Rezepte, Crafting-Tisch)
- [Nice-to-have] Werkzeug-Haltbarkeit

### Spieler-Erweiterungen
- [Erledigt] Gesundheits-System (HP, Schaden, Regeneration, Fallschaden)
- [Erledigt] Hunger-System (beeinflusst Regeneration, Verhungern)
- [Erledigt] Entity-Basisklasse (`Entity/`, Player erbt davon - Phase-7-ready)
- [Nice-to-have] Erfahrungspunkte + Level
- [Nice-to-have] Spieler-Tod + Respawn

### Wasser-Simulation
- [Mittel] Level-System (1-8), Cellular Automata
- [Nice-to-have] Volumen-Erhaltung, Stroemung

### Sound
- [Nice-to-have] Sound-System (OpenAL via Silk.NET)
- [Nice-to-have] Umgebungsgeraeusche pro Klimazone
- [Nice-to-have] Block-Sounds (abbauen, platzieren)
- [Nice-to-have] Spieler-Sounds (Schritte, Sprung, Landen)
- [Nice-to-have] Sounds je nach Block unterschiedlich

---

## Phase 7 - Entity-System und Welt beleben

> Voraussetzung: Inventar-System [Erledigt]

### Entity-System Architektur
- [Erledigt] EntityManager in GameContext (Frustum-Culling, Spatial Hashing)
- [Erledigt] Entity-Rendering (Voxel-Modelle + Entity-Atlas, batched pro Modelltyp)
- [Mittel] Entity-Kollision mit Terrain (AABB wie Spieler)
- [Erledigt] Entity-Spawning pro Klimazone
- [Nice-to-have] Entity-Persistenz
- [Nice-to-have] LOD fuer Entities

### Vegetation
- [Mittel] Gras + Blumen als Billboard-Sprites
- [Nice-to-have] Buesche, Pilze, Farne pro Klimazone
- [Nice-to-have] Gefaellte Baeume droppen Holz-Items

### Tiere
- [Mittel] Tier-Entity (Idle, zufaellige Bewegung, Flucht)
- [Mittel] Tiere pro Klimazone (Schafe, Woelfe, Kamele, Papageien, Fische...)
- [Mittel] Ressourcen droppen
- [Nice-to-have] Tag/Nacht Verhalten, Herden, Zucht

### NPCs und Feinde
- [Nice-to-have] NPC-Entity (Haendler, Dialog, Handel)
- [Nice-to-have] Feind-Entity (Aggro, Pathfinding, Kampf)
- [Nice-to-have] Dorf-Generierung

### Erweiterbarkeits-Architektur
- [Mittel] Component System (`IComponent`, Physics/Health/AI/Drop/Sprite)
- [Mittel] EntityRegistry + BiomeRegistry
- [Mittel] ItemRegistry (Items als eigene Definitionen)
- [Nice-to-have] Data-Driven Content (Blocks/Entities/Biomes aus JSON)
- [Nice-to-have] Mod-Support (`Mods/` Ordner, `mod.json` Manifest)

---

## Phase 8 - Multiplayer (optional, langfristig)

- [Nice-to-have] Option A: SpacetimeDB (C# Module, weniger Boilerplate, Free Tier)
- [Nice-to-have] Option B: ASP.NET Core + SignalR + MessagePack (volle Kontrolle)
- [Nice-to-have] Gemeinsam: Client-Side Prediction, Chunk-Streaming, Nametags, Chat

---

## Empfohlene Reihenfolge
```
Erledigt:    Phase 5 - Chunk-Serialisierung
Erledigt:    Phase 6 - Inventar-Fenster + Gesundheit
Jetzt:       Phase 7 - Entity-System + Tiere
Langfristig: Phase 8 - Multiplayer
```
