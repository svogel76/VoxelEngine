# AGENTS.md — KI-Arbeitsanweisungen für VoxelEngine

## Pflichtlektüre vor jeder Aufgabe
1. **`CLAUDE.md`** — Architektur, Konventionen, nächste Schritte
2. **`Backlog.md`** — priorisierte Features und offene Aufgaben

## Git-Workflow
- `git pull` auf main ausführen bevor eine Aufgabe beginnt
- Neuen Branch erstellen: `feature/kurze-beschreibung` oder `fix/kurze-beschreibung`
- Niemals direkt auf `main` pushen — immer als Pull Request
- Sprache im Code: **Englisch** (Bezeichner, Kommentare)
- Kommunikation mit Steffen: **Deutsch**

## Build & Test
- Nach jeder Änderung: `dotnet build VoxelEngine.Launcher/VoxelEngine.Launcher.csproj`
- Vor jedem PR: `dotnet test VoxelEngine.Tests/VoxelEngine.Tests.csproj`
- Build und Tests müssen grün sein — kein PR mit Fehlern

## Unit Tests
- Test-Projekt: `VoxelEngine.Tests/` (xUnit + FluentAssertions)
- **Neue Logik in `World/` bekommt immer Tests**
- **Neue Rendering-Features bekommen ebenfalls Tests wo sinnvoll**
- Tests nach AAA-Muster strukturieren (Arrange / Act / Assert)
- Keine Magic Numbers in Tests — Konstanten aus den echten Klassen verwenden
- Bestehende Tests niemals löschen oder deaktivieren

## Pull Request Regeln
- PR-Titel auf Deutsch
- PR-Beschreibung: Was wurde geändert, warum, welche Dateien
- CLAUDE.md aktualisieren: Nächste Schritte anpassen
- Backlog.md aktualisieren: Erledigte Punkte als [Erledigt] markieren, neue Erkenntnisse ergänzen

## Stopp-Bedingungen
- Architekturentscheidung betrifft mehrere Systeme → nachfragen
- Build nach 2 Versuchen nicht grün → stoppen und melden
- Tests nach Fix noch rot → stoppen und melden, nicht weiter patchen
- Neue NuGet-Abhängigkeiten → immer erst Rückfrage

## Kritische Regeln (aus CLAUDE.md, zur Erinnerung)
- `World/` darf NIEMALS Silk.NET importieren
- GL-Calls nur im Hauptthread
- Keine Magic Numbers — alles über EngineSettings
- Nach Block-Änderungen: Nachbar-Chunks an Grenzen neu aufbauen
- Neue Block-Eigenschaften zentral in BlockRegistry + BlockDefinition
- Neue Debug-Kommandos als eigene Klasse in `Core/Debug/Commands/`
- Klimazonen-Parameter (Nebel, Bäume, Terrain) gehören in `Assets/climates/*.json` — nie hardcoded