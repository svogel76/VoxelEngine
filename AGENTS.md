# AGENTS.md – KI-Assistenz im VoxelEngine-Projekt

Dieses Dokument beschreibt das Projekt kurz für KI-Assistenten (z. B. Codex, Claude), die im Repo arbeiten.

## Projektbeschreibung

**VoxelEngine** ist eine Voxel-Engine im Minecraft-Stil, geschrieben in **C# 13** auf **.NET 10.0** mit **Silk.NET** als OpenGL-Binding. Ziel ist eine performante, erweiterbare Engine mit folgenden Schwerpunkten:

- 🌍 **Prozedurale Weltgenerierung** via OpenSimplex2-Rauschen (FastNoiseLite)
- 🧱 **Greedy Meshing** mit Ambient-Occlusion und Diagonalflip-Korrektur
- ⚡ **Multithreading** – Chunk-Generierung & Meshing im Hintergrund, GPU-Upload im Hauptthread
- 🌅 **Dynamischer Tageszyklus** – 24-h-Uhr, 8-Keyframe-Farbkurven, Sonne/Mond/Sterne
- 💧 **Zwei-Pass-Transparenz** – Wasser, Glas, Eis mit Alpha-Blending
- 🔭 **Frustum-Culling** nach Gribb-Hartmann
- 🖥️ **Debug-Konsole** mit 9 Befehlen und Bitmap-Font-HUD

## Wichtige Dateien

| Datei | Zweck |
|---|---|
| `VoxelEngine/Claude.md` | Architekturentscheidungen & Konventionen für KI-Assistenz |
| `VoxelEngine/Backlog.md` | Offene Aufgaben & priorisierte Features |
| `README.md` | Vollständige Projektdokumentation |

## Für KI-Agenten

- **Vor jeder Aufgabe zuerst `git pull`** auf dem main Branch ausführen
- Dann neuen Feature-Branch erstellen
- Niemals auf einem veralteten Stand arbeiten
- **Immer zuerst `VoxelEngine/Claude.md` lesen** – dort stehen Konventionen, die eingehalten werden müssen
- Änderungen **nicht direkt auf `main` pushen** – immer als Pull Request
- Nach Code-Änderungen: **Build prüfen** mit `dotnet build VoxelEngine/VoxelEngine.csproj`
- Sprache im Code: **Englisch** (Bezeichner, Kommentare); Kommunikation mit Steffen: **Deutsch**

### Stopp-Bedingungen
- Bei Architekturentscheidungen die mehrere Systeme betreffen → nachfragen
- Wenn der Build nach 2 Versuchen nicht grün wird → stoppen und melden
- Keine bestehenden Tests löschen oder deaktivieren
- Keine Abhängigkeiten (NuGet Packages) ohne Rückfrage hinzufügen
- Keine bestehenden Tests löschen oder deaktivieren  ← bereits vorhanden, bleibt
- Wenn Tests nach dem Fix noch rot sind → stoppen und melden (nicht weiter patchen)

### Pull Request Regeln
- Branch-Name: feature/kurze-beschreibung oder fix/kurze-beschreibung
- PR-Titel auf Deutsch
- PR-Beschreibung enthält: Was wurde geändert, warum, welche Dateien
- Build muss grün sein bevor PR geöffnet wird
- `dotnet test` muss grün sein (zusätzlich zu `dotnet build`)

### Kritische Architekturregeln
- World/ Verzeichnis darf NIEMALS Silk.NET importieren
- GL-Calls nur im Hauptthread
- Keine Magic Numbers – alle Konfiguration über EngineSettings
- Nach Block-Änderungen: Nachbar-Chunks an Borders neu aufbauen

### Unit Tests
- Test-Projekt: `VoxelEngine.Tests/` (xUnit + FluentAssertions)
- Tests ausführen: `dotnet test VoxelEngine.Tests/VoxelEngine.Tests.csproj`
- **Nach jeder Implementierung Tests ausführen** — kein PR ohne grüne Tests
- **Neue Logik in `World/` bekommt immer Tests** – insbesondere:
  - Neue Block-Eigenschaften in BlockRegistry
  - Neue Gameplay-Mechaniken (Physik, Kollision, Zeitlogik)
  - Neue Chunk-Operationen (Dirty-Flag, Serialisierung, Edits)
- Tests strukturieren nach AAA-Muster (Arrange / Act / Assert)
- Keine Magic Numbers in Tests — Konstanten aus den echten Klassen verwenden
- Kein Mocking nötig — World/ ist pure C# ohne Framework-Abhängigkeiten
