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

- **Immer zuerst `VoxelEngine/Claude.md` lesen** – dort stehen Konventionen, die eingehalten werden müssen
- Änderungen **nicht direkt auf `main` pushen** – immer als Pull Request
- Nach Code-Änderungen: **Build prüfen** mit `dotnet build VoxelEngine/VoxelEngine.csproj`
- Sprache im Code: **Englisch** (Bezeichner, Kommentare); Kommunikation mit Steffen: **Deutsch**
