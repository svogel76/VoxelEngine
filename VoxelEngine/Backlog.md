# VoxelEngine — Backlog

## Legende
- 🔴 Hoch — blockiert andere Features
- 🟡 Mittel — wichtig für Spielgefühl
- 🟢 Nice-to-have — Qualität & Polish

---

## Phase 2.5 — Debug & Entwicklungswerkzeuge

- 🟢 Konsolen-Output farbig
- 🟢 Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
- "weather sunny/rain/snow/storm"    → Wetter (Phase 4)
- "season spring/summer/autumn/winter" → Jahreszeit (Phase 4)
- "noise seed x"                     → Seed ändern
- "entity list / spawn / kill"       → Entity-System (Phase 7)

---

## Phase 3 — Klimazonen & World Generation

### Klimazonen-Feintuning
- 🟡 Polarregion + Tundra (sehr kalt, Permafrost, Eis)
- 🟡 Mediterran (warm/trocken, Kalkstein)

### Höhenzonen
- 🟡 Küste (Y=64-68, Sand/Kies Übergang)
- 🟡 Waldgrenze (pro Klimazone unterschiedlich)
- 🟡 Schneegrenze (pro Klimazone unterschiedlich)
- 🟡 Gipfelzone (reiner Stein/Schnee)

### Gewässer — Generierung
- 🟡 Seen (Mulden auffüllen)
- 🟡 Gletscher (Polar/Tundra Klimazonen)
- 🟢 Flüsse (Pfad-Algorithmus von Bergen zum Meer)
- 🟢 Oasen (Wasser in Wüsten-Klimazone)

### Gewässer — Simulation
- 🟡 Level-System (1-8), Cellular Automata
- 🟢 Volumen-Erhaltung, Strömung
- 🟢 Spieler-Interaktion (Block entfernen leitet Wasser um)

### Unterirdisches
- 🟡 Höhlen (3D Noise Density-Field)
- 🟡 Erzvorkommen pro Klimazone
- 🟢 Dungeons, Katakomben, Verlorene Städte

### Oberflächen-Strukturen
- 🟢 Ruinen, Dörfer, Burgen pro Klimazone
- 🟢 Schablonen-Format (externe Datei)

---

## Phase 4 — Rendering & Visuals

- 🟡 Jahreszeiten (Farb-Tint, Tageslänge — DayCount vorhanden)
- 🟡 Wetter-Zustandsautomat (Sonnig→Bewölkt→Regen/Schnee)
- 🟡 Regen/Schnee Partikel
- 🟢 Wolken (prozedural, ziehen mit Wind)
- 🟢 Sonnen-Halo / Atmosphären-Streuung
- 🟢 Distant Horizons (LOD-Silhouetten)
- 🟢 Billboard-Sprites für Vegetation (Gras, Blumen, Wippen im Wind)

---

## Phase 5 — Engine & Architektur

- 🟡 Chunk-Serialisierung (Welt speichern und laden — PlayerEdits-Grundlage vorhanden)
- 🟢 Asset-Management System
- 🟢 LOD (entfernte Chunks vereinfacht)

---

## Phase 6 — Gameplay & Simulation

### Inventar-System
- 🟡 Item-Icons in Hotbar (Top-Face aus ArrayTexture)
- 🟡 Vollständiges Inventar-Fenster (4×9 Slots, Tab öffnen)
- 🟢 Crafting-System (Rezepte, Crafting-Tisch)
- 🟢 Ausrüstungs-Slots (Rüstung, Werkzeug)
- 🟢 Werkzeug-Haltbarkeit

### Spieler-Erweiterungen
- 🟡 Gesundheits-System (HP, Schaden, Regeneration)
- 🟡 Hunger-System (beeinflusst Regeneration)
- 🟢 Erfahrungspunkte + Level
- 🟢 Spieler-Tod + Respawn

### Wasser-Simulation
- 🟡 Level-System (1-8), Cellular Automata
- 🟢 Volumen-Erhaltung, Strömung

### Sound
- 🟢 Sound-System (OpenAL via Silk.NET)
- 🟢 Umgebungsgeräusche pro Klimazone
- 🟢 Block-Sounds (abbauen, platzieren)
- 🟢 Spieler-Sounds (Schritte, Sprung, Landen)

---

## Phase 7 — Entity-System & Welt beleben

> Voraussetzung: Inventar-System

### Entity-System Architektur
- 🔴 Entity Basisklasse (Position, Velocity, BoundingBox, Update(), Render())
- 🔴 EntityManager in GameContext (Frustum-Culling, Spatial Hashing)
- 🔴 Entity-Rendering (Billboard-Sprites oder Voxel-Modelle)
- 🟡 Entity-Kollision mit Terrain (AABB wie Spieler)
- 🟡 Entity-Spawning pro Klimazone
- 🟢 Entity-Persistenz
- 🟢 LOD für Entities

### Vegetation
- 🟡 Gras + Blumen als Billboard-Sprites
- 🟢 Büsche, Pilze, Farne pro Klimazone
- 🟢 Gefällte Bäume droppen Holz-Items

### Tiere
- 🟡 Tier-Entity (Idle, zufällige Bewegung, Flucht)
- 🟡 Tiere pro Klimazone (Schafe, Wölfe, Kamele, Papageien, Fische...)
- 🟡 Ressourcen droppen
- 🟢 Tag/Nacht Verhalten, Herden, Zucht

### NPCs & Feinde
- 🟢 NPC-Entity (Händler, Dialog, Handel)
- 🟢 Feind-Entity (Aggro, Pathfinding, Kampf)
- 🟢 Dorf-Generierung

### Erweiterbarkeits-Architektur
- 🟡 Component System (IComponent, Physics/Health/AI/Drop/Sprite)
- 🟡 EntityRegistry + BiomeRegistry
- 🟡 ItemRegistry (Items als eigene Definitionen)
- 🟢 Data-Driven Content (Blocks/Entities/Biomes aus JSON)
- 🟢 Mod-Support (Mods/ Ordner, mod.json Manifest)

---

## Phase 8 — Multiplayer (optional, langfristig)

- 🟢 Option A: SpacetimeDB (C# Module, weniger Boilerplate, Free Tier)
- 🟢 Option B: ASP.NET Core + SignalR + MessagePack (volle Kontrolle)
- 🟢 Gemeinsam: Client-Side Prediction, Chunk-Streaming, Nametags, Chat

---

## Empfohlene Reihenfolge
```
Jetzt:       Phase 5 — Chunk-Serialisierung
Danach:      Phase 6 — Inventar-Fenster + Gesundheit
Dann:        Phase 7 — Entity-System + Tiere
Langfristig: Phase 8 — Multiplayer
```