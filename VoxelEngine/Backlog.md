# VoxelEngine — Backlog

## Legende
- 🔴 Hoch — blockiert andere Features
- 🟡 Mittel — wichtig für Spielgefühl
- 🟢 Nice-to-have — Qualität & Polish
- ✅ Erledigt

---

## Phase 2 — Welt & Terrain ✅ Abgeschlossen

- ✅ Chunk-Datenstruktur (16×256×16, Dictionary)
- ✅ Naive Culling Meshing → ersetzt durch Greedy Meshing
- ✅ Backface Culling (CCW Winding Order)
- ✅ Perlin Noise Terrain-Generation
- ✅ Chunk-Manager (dynamisches Laden/Entladen, Hysterese)
- ✅ Sichtweite konfigurierbar (RenderDistance)

---

## Phase 2.5 — Debug & Entwicklungswerkzeuge

- ✅ GameContext (zentraler Container für alle Systeme)
- ✅ Debug-Konsole (F1, Overlay, Command-Registry)
- ✅ Basis-Kommandos: help, pos, tp, wireframe, chunk info, renderdistance, skybox, time
- ✅ Erweiterbare Command-Registry (ICommand Interface)
- 🟡 Konsolen-History (Pfeiltasten blättern)
- 🟡 Autocomplete (Tab vervollständigt Kommando-Namen)
- 🟢 Konsolen-Output farbig (Fehler rot, Erfolg grün)
- 🟢 Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
- "weather sunny/rain/snow/storm"      → Wetter (Phase 4)
- "season spring/summer/autumn/winter" → Jahreszeit (Phase 4)
- "climate info"                       → Klimazone (Phase 3)
- "noise seed x"                       → Seed ändern + Welt neu generieren (Phase 3)

---

## Phase 3 — Klimazonen & World Generation

### Klimazonen-Architektur
- 🔴 ClimateSystem (Temperatur + Feuchtigkeit pro Block-Position)
- 🔴 ClimateZone Klasse (ersetzt BiomeDefinition)
- 🔴 Übergangs-Interpolation zwischen Klimazonen (~20 Blöcke)
- 🔴 Meeresspiegel (Y=64, Wasser-Block)

### Klimazonen
- 🟡 Polarregion, Tundra, Taiga, Steppe
- 🟡 Gemäßigt, Mediterran, Savanne, Wüste, Tropen

### Höhenzonen
- 🟡 Küste, Tiefland, Waldgrenze, Schneegrenze, Gipfelzone

### Gewässer — Generierung
- 🟡 Wasser-Block (transparent)
- 🟡 Meere, Seen, Gletscher
- 🟢 Flüsse, Oasen, Sumpfgebiete

### Gewässer — Simulation
- 🟡 Level-System (1-8), Cellular Automata
- 🟢 Volumen-Erhaltung, Strömung

### Unterirdisches
- 🟡 Höhlen (3D Noise Density-Field)
- 🟡 Erzvorkommen pro Klimazone
- 🟢 Dungeons, Katakomben, Verlorene Städte

### Oberflächen-Strukturen
- 🟢 Bäume pro Klimazone (Schablone + L-System)
- 🟢 Ruinen, Dörfer, Burgen, Tempel
- 🟢 Struktur-Seeds (Chunk-Grenzen übergreifend)

---

## Phase 4 — Rendering & Visuals

- ✅ Textur-Atlas → ArrayTexture (Texture2DArray, 8 Schichten)
- ✅ Greedy Meshing (3-Achsen-Sweep, AO-korrekter Merge)
- ✅ Frustum Culling (FrustumCuller, Gribb-Hartmann)
- ✅ Ambient Occlusion (VertexAO, Diagonal-Flip, Z-Fighting Fix)
- ✅ Skybox (prozeduraler Gradient, Zenith/Horizont/Boden)
- ✅ WorldTime (Time, DayCount, MoonPhase, TimeScale)
- ✅ SkyColorCurve (7 Keyframes, Interpolation)
- ✅ Sonne + Mond (Billboard Quads, Mondphasen, Opacity-Fade)
- ✅ Diffuse Beleuchtung (Sonnenrichtung → Shader-Uniform → Flächen-Helligkeit)
- 🟡 Transparente Blöcke (Wasser, Glas — eigener Render-Pass)
- ✅ Sterne (Nachthimmel — Partikel oder Billboard-Quads)
- ✅ Fog (Entfernungs-Nebel an Chunk-Grenzen)
- 🟢 Sonnen-Halo / Atmosphären-Streuung (Horizont heller wenn Sonne nah)
- 🟢 Wolken (prozedural, ziehen mit Wind)
- 🟢 Ambient Occlusion verfeinern (SSAO als Post-Process)

### Tageszeit & Jahreszeiten
- 🟡 Jahreszeiten-Zyklus (alle 90 Tage, DayCount bereits vorhanden)
- 🟡 Tageslänge variiert mit Jahreszeit
- 🟡 Gras/Laub Farb-Tint pro Jahreszeit
- 🟢 Bäume verlieren Laub im Herbst/Winter

### Wetter
- 🟡 Wetter-Zustandsautomat (Sonnig → Bewölkt → Regen/Schnee)
- 🟡 Regen/Schnee Partikel
- 🟡 Dynamischer Fog bei Regen/Nebel
- 🟢 Gewitter (Blitz, Donner)
- 🟢 Schnee-Akkumulation auf Oberflächen

### Distant Horizons
- 🟢 Heightmap-only für entfernte Bereiche (ohne Chunk laden)
- 🟢 LOD-Stufen (voll / vereinfacht / Silhouette)
- 🟢 Nahtloser Übergang in Skybox

---

## Phase 5 — Engine & Architektur

- 🔴 Multithreading — Chunk-Generierung im Background-Thread
- 🔴 Block-Interaktion (Blöcke setzen und abbauen)
- 🟡 Dirty-Flag pro Chunk (Mesh-Rebuild nur bei Änderung)
- 🟡 Chunk-Serialisierung (Welt speichern und laden)
- 🟡 Block-Typen Registry (statt hardcodierter byte-Konstanten)
- 🟢 Asset-Management System
- 🟢 LOD (entfernte Chunks vereinfacht)

---

## Phase 6 — Gameplay & Simulation

- 🔴 Spieler-Entity (trennt Kamera von Spieler-Position)
- 🔴 Spieler-Kollision mit Terrain
- 🔴 Gravitation und Sprung
- 🟡 Wasser-Simulation (Cellular Automata)
- 🟡 Inventar-System
- 🟢 Sound-System (OpenAL via Silk.NET)
- 🟢 Crafting-System
- 🟢 Mobs / NPCs

---

## Architektur-Notizen
- World/ bleibt frei von Silk.NET → Portabilität
- WorldTime ist zentrale Variable → Tageszeit, Wetter, Jahreszeit
- Biome/Klimazonen matchen nicht auf Chunk-Grenzen → BiomeWeights pro Block
- Wasser-Generierung und Simulation sind getrennte Systeme
- Struktur-Seeds: deterministisch, Chunk-Grenzen übergreifend
- Spieler-Entity von Kamera trennen vor Phase 6
- Block-Interaktion: Dirty-Flag + max N Chunk-Rebuilds pro Frame