# VoxelEngine — Backlog

## Legende
- 🔴 Hoch — blockiert andere Features
- 🟡 Mittel — wichtig für Spielgefühl
- 🟢 Nice-to-have — Qualität & Polish
- ✅ Erledigt

---

## Phase 2 — Welt & Terrain (aktuell)

- ✅ Chunk-Datenstruktur (16×256×16, Dictionary)
- ✅ Naive Culling Meshing
- ✅ Backface Culling
- ✅ Perlin Noise Terrain-Generation
- ✅ Chunk-Manager — dynamisches Laden/Entladen um Spielerposition
- ✅ Chunk-Mesh Rebuild bei Block-Änderung
- ✅ Sichtweite konfigurierbar (render distance)

---

## Phase 2.5 — Debug & Entwicklungswerkzeuge
> Früh implementieren — vereinfacht alle weiteren Phasen erheblich

- ✅ GameContext (zentraler Container für alle Systeme)
       Voraussetzung für mächtige Kommandos, räumt Engine.cs auf
- ✅ Debug-Konsole (Overlay, Command-Parser, Registry)
       Öffnen mit § oder F1
- ✅ Basis-Kommandos:
       "help"              → alle Kommandos auflisten
       "pos"               → Spieler-Position anzeigen
       "tp x y z"          → Teleportieren
       "fly"               → Fly-Modus umschalten
       "wireframe"         → Wireframe-Rendering toggle
       "chunk info x z"    → Chunk-Daten anzeigen
       "chunk rebuild"     → Alle Meshes neu generieren
       "render distance x" → Sichtweite ändern
- ✅ Erweiterbare Command-Registry (ICommand Interface)
- 🟡 Konsolen-History (Pfeiltasten blättern durch letzte Kommandos)
- 🟡 Autocomplete (Tab vervollständigt Kommando-Namen)
- 🟢 Konsolen-Output (farbige Fehlermeldungen, Erfolgs-Meldungen)
- 🟢 Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
> Werden bei Implementierung des jeweiligen Features ergänzt
- "time 0-24"                      → Tageszeit (Phase 4)
- "weather sunny/rain/snow/storm"  → Wetter (Phase 4)
- "season spring/summer/autumn/winter" → Jahreszeit (Phase 4)
- "biome info"                     → Biom-Info (Phase 3)
- "noise seed x"                   → Seed ändern + Welt neu generieren (Phase 3)

---

## Phase 3 — Klimazonen & World Generation

### Klimazonen-Architektur
> Realistisches System angelehnt an Köppen-Klimaklassifikation.
> Zwei Parameter bestimmen die Klimazone: Temperatur + Feuchtigkeit.
> Ersetzt das einfache Biom-Noise System.

- 🔴 ClimateSystem (Temperatur + Feuchtigkeit pro Block-Position)
       Temperatur: primär Z-Koordinate (Breitengrad) + leichter Noise
       Feuchtigkeit: eigenständiger Noise-Wert
       Höhen-Einfluss: Berge kühlen Temperatur ab
- 🔴 ClimateZone Klasse (ersetzt BiomeDefinition)
       Felder: NoiseSettings, BlockTypes, TreeLine, SnowLine
       Lookup: (temperature, humidity) → ClimateZone
- 🔴 Übergangs-Interpolation zwischen Klimazonen
       Keine harten Grenzen — sanfter Übergang über ~20 Blöcke
- 🔴 Meeresspiegel (fester Y-Wert, Wasser-Block unter dieser Höhe)

### Klimazonen (angelehnt an Erde)
- 🟡 Polarregion   (sehr kalt → Schnee, Eis, kein Bewuchs)
- 🟡 Tundra        (kalt + trocken → karges Gras, Flechten, Permafrost)
- 🟡 Taiga         (kalt + feucht → Nadelwald, Moos, Farne)
- 🟡 Steppe        (mittel + trocken → trockenes Gras, wenig Bäume)
- 🟡 Gemäßigt      (mittel + feucht → Laubwald, Gras, Europa-Feeling)
- 🟡 Mediterran    (mittel + warm/trocken → Olivenbäume, Kalkstein)
- 🟡 Savanne       (heiß + mäßig feucht → Gras, vereinzelte Bäume)
- 🟡 Wüste         (heiß + trocken → Sand, Kalkstein, Kakteen)
- 🟡 Tropen        (heiß + feucht → dichter Wald, Palmen, Bambus)

### Höhenzonen (überlagern Klimazonen)
- 🟡 Meeresgrund   (unter Wasser → Sand, Kies, Korallen)
- 🟡 Küste         (Y=64-68 → Sand/Kies Übergangszone)
- 🟡 Tiefland      (Normales Terrain der jeweiligen Klimazone)
- 🟡 Waldgrenze    (pro Klimazone unterschiedlich → keine Bäume mehr)
- 🟡 Schneegrenze  (pro Klimazone unterschiedlich → Schnee beginnt)
- 🟡 Gipfelzone    (reiner Stein/Schnee, kein Bewuchs)

### Terrain-Grundformen
- 🟡 Ebene         (flach, gemäßigte/Savanne Klimazone)
- 🟡 Hügel         (mittlere Amplitude, gemäßigt/Taiga)
- 🟡 Berge         (hohe Amplitude, alle Klimazonen)
- 🟡 Ozean         (unter Meeresspiegel, Sand/Kies am Boden)
- 🟢 Fjorde        (schmale Meeresarme in Bergregionen)
- 🟢 Atolle        (flache Inseln in tropischen Ozeanen)

### Gewässer — Generierung
- 🟡 Wasser-Block (transparent, eigene Rendering-Logik)
- 🟡 Meere (Terrain unter Meeresspiegel = Wasser aufgefüllt)
- 🟡 Seen (Mulden im Terrain werden aufgefüllt)
- 🟡 Gletscher (Eis-Blöcke in Polar/Tundra Klimazonen)
- 🟢 Flüsse (Pfad-Algorithmus von Bergen zum Meer)
- 🟢 Oasen (Wasser in Wüsten-Klimazone)
- 🟢 Sumpfgebiete (flaches Wasser in feuchten Klimazonen)

### Gewässer — Simulation (Laufzeit)
> Erst sinnvoll wenn Block-Interaktion und transparentes Rendering stehen
- 🟡 Wasser-Block mit Level-System (1-8, simuliert Gefälle)
- 🟡 Cellular Automata Simulation
       Regeln: runter → seitlich → Gefälle folgen
- 🟢 Volumen-Erhaltung (Wasser verschwindet nicht)
- 🟢 Spieler-Interaktion (Block entfernen leitet Wasser um)
- 🟢 Strömung (Wasser-Blöcke haben Fließrichtung)

### Wetter pro Klimazone
- 🟢 Polarregion: Schneestürme, Permafrost
- 🟢 Tundra: langer Winter, kurzer Sommer
- 🟢 Taiga: Schnee im Winter, Regen im Sommer
- 🟢 Gemäßigt: wechselhaft, alle Jahreszeiten
- 🟢 Mediterran: trockener Sommer, milder Winter
- 🟢 Savanne: Trocken- und Regenzeit
- 🟢 Wüste: selten Regen, Sandstürme
- 🟢 Tropen: häufige Gewitter, Monsun

### Unterirdisches
- 🟡 Höhlen via 3D Noise (Density-Field Ansatz)
- 🟡 Erzvorkommen (Noise-Einschlüsse pro Klimazone)
       Wüste: Sandstein, Kupfer / Taiga: Eisen, Kohle / Berge: Gold, Diamant
- 🟢 Dungeons (prozedurale Räume, BSP-Baum Algorithmus)
- 🟢 Katakomben (unter Ruinen/Dörfern, verbunden mit Höhlen)
- 🟢 Antike Tunnel (lineare unterirdische Strukturen)
- 🟢 Verlorene Städte (große unterirdische Komplexe)
- 🟢 Minenschächte (lineare Strukturen tief im Gestein)

### Oberflächen-Strukturen

#### Natürlich
- 🟢 Bäume — Schablone pro Klimazone
       Tropen: Palmen, Bananenstauden
       Gemäßigt: Eiche, Birke, Buche
       Taiga: Fichte, Kiefer, Tanne
       Wüste: Kaktus, Dornenstrauch
       Tundra: Zwergbirke, Flechten
- 🟢 Bäume — L-System für organische Verzweigung
- 🟢 Felsen / Steinformationen (pro Klimazone)
       Wüste: Sandsteinbögen / Berge: Felsnadeln / Küste: Klippen
- 🟢 Überhänge / Klippen (entstehen mit 3D Terrain-Noise)
- 🟢 Gletscher / Eisformationen (Polar + hohe Berge)
- 🟢 Korallenriffe (tropische Flachwasserzone)

#### Zivilisation
- 🟢 Ruinen (Schablone + Verfall-Algorithmus)
       Typen abhängig von Klimazone:
       Wüste: Pyramiden, Tempel / Gemäßigt: Burgen, Türme
       Tropen: Dschungeltempel / Tundra: Steinkreise
- 🟢 Dörfer (BSP-Baum, Straßennetz, Häuser pro Klimazone)
- 🟢 Burgen / Festungen (auf Berggipfeln, gemäßigte Zone)
- 🟢 Hafen-Strukturen (nur wo Land auf Wasser trifft)
- 🟢 Alte Straßen / Pfade (Pfad-Algorithmus zwischen Punkten)
- 🟢 Nomaden-Lager (Wüste, Tundra — temporär wirkende Strukturen)

### Struktur-System Architektur
- 🟡 Structure Seeds (deterministisch, Chunk-Grenzen übergreifend)
- 🟡 Struktur-Registry (welche Struktur in welcher Klimazone?)
- 🟡 Terrain-Validierung vor Platzierung
       z.B. Dorf nur auf flachem Terrain der passenden Klimazone
- 🟢 Schablonen-Format (externe Datei statt hardcodierter Blöcke)

---

## Phase 4 — Rendering & Visuals

- ✅ Textur-Atlas (verschiedene Texturen pro Block-Typ)
- 🟡 Greedy Meshing (Optimierung — große Quads statt viele kleine)
- ✅ Frustum Culling (Chunks außerhalb Sichtfeld nicht rendern)
- 🟡 Transparente Blöcke (Wasser, Glas — eigener Render-Pass)
- 🟡 Ambient Occlusion (weiche Schatten an Block-Kanten)

### Tageszeit
- 🟡 Sonnen-Zyklus (Winkel → Lichtfarbe + Helligkeit)
- 🟡 Dynamische Skybox (Nacht/Tag/Sonnenauf-untergang)
- 🟡 Ambient Light System (Shader-Uniform)
- 🟢 Sterne (Nachthimmel)
- 🟢 Mond (Mondphasen)
- 🟢 Schatten-Richtung (mit Sonnenwinkel)

### Wetter
- 🟡 Wetter-Zustandsautomat
       (Sonnig → Bewölkt → Regen/Schnee → Aufklaren)
- 🟡 Regen-Partikel (Partikel-System)
- 🟡 Schnee-Partikel (Partikel-System)
- 🟡 Wetter-Wahrscheinlichkeit pro Biom
- 🟡 Dynamischer Fog (dichter bei Regen/Nebel)
- 🟢 Gewitter (Blitz-Effekte, Donner)
- 🟢 Nebel (morgens in Tälern und an Gewässern)
- 🟢 Schnee-Akkumulation (Schnee-Blöcke auf Oberflächen)
- 🟢 Wasser-Gefrieren (Seen im Winter)
- 🟢 Nasse Oberflächen (Shader-Effekt bei Regen)

### Jahreszeiten
- 🟡 Jahreszeiten-Zyklus (alle 90 Tage eine Saison)
- 🟡 Gras/Laub Farb-Tint pro Jahreszeit
       (Frühling: hellgrün, Sommer: sattgrün,
        Herbst: orange/rot, Winter: grau/weiß)
- 🟡 Tageslänge variiert mit Jahreszeit
- 🟢 Bäume verlieren Laub im Herbst/Winter
- 🟢 Blüten im Frühling
- 🟢 Wetter-Wahrscheinlichkeit ändert sich mit Jahreszeit

### Zeit-Architektur
- 🟡 WorldTime als zentrale Variable
       (TimeOfDay, DayCount, Year, Season)
- 🟡 Konfigurierbare Tages-/Jahres-Geschwindigkeit
       in EngineSettings
- 🟢 Zeitsteuerung per Debug-Konsole
       (Zeit beschleunigen, Tag/Nacht erzwingen)

### Distant Horizons
- 🟢 Heightmap-only Berechnung für entfernte Bereiche
       (Noise-Funktion aufrufen ohne Chunk zu laden)
- 🟢 LOD-Stufen (3 Schichten: voll / vereinfacht / Silhouette)
- 🟢 Übergangs-Nebel zwischen LOD-Stufen
- 🟢 Silhouetten-Mesh Generator
- 🟢 Nahtloser Übergang in Skybox
> Hinweis: Erst sinnvoll nach Chunk-Manager, Skybox und Fog

---

## Phase 5 — Engine & Architektur

- 🔴 Multithreading — Chunk-Generierung im Background-Thread
- 🔴 Block-Interaktion (Blöcke setzen und abbauen)
- 🟡 Chunk-Serialisierung (Welt speichern und laden)
- 🟡 Block-Typen Registry (statt hardcodierter byte-Konstanten)
- 🟢 LOD (Level of Detail — entfernte Chunks vereinfacht)
- 🟢 Asset-Management System
- 🟢 Multi-Backend Überlegung (Abstraktion für Vulkan)

---

## Phase 6 — Gameplay & Simulation

- 🔴 Spieler-Kollision mit Terrain
- 🔴 Gravitation und Sprung
- 🟡 Wasser-Simulation (Cellular Automata, Level-System)
- 🟡 Inventar-System
- 🟢 Crafting-System
- 🟢 Mobs / NPCs (aufwändig, eigenes System)

---

## Architektur-Notizen
- World/ bleibt frei von Silk.NET → Portabilität auf andere C# Frameworks
- Biome matchen nicht auf Chunk-Grenzen → BiomeWeights pro Block-Position
- NoiseSettings als eigene Klasse → später eine Instanz pro Biom
- Wasser-Generierung und Wasser-Simulation sind getrennte Systeme
- Flüsse: Pfad-Algorithmus zur Generierung, Cellular Automata zur Laufzeit
- Struktur-Seeds: deterministisch, Chunk-Grenzen übergreifend
- WorldTime: eine einzige Variable treibt Tageszeit, Wetter, Jahreszeit