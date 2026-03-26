## Konzept: Spieler-Entity

Die Spieler-Entity ist ein einfacher Datenbehälter mit etwas Logik. Kein Silk.NET, kein OpenGL — pure C# in `World/`.

```
Player
├── Position        (Vector3 — Füße des Spielers)
├── Velocity        (Vector3 — Bewegungsgeschwindigkeit)
├── IsOnGround      (bool — für Sprung-Logik später)
├── BoundingBox     (0.6 × 1.8 × 0.6 Blöcke — wie Minecraft)
└── EyeOffset       (0 + 1.62 Blöcke — Kamera-Position)
```

## Die wichtige Designentscheidung: Koordinaten

Der Spieler steht mit den **Füßen** auf dem Boden. Die Kamera ist auf Augenhöhe:

```
Y + 1.80  ┬  Kopf
Y + 1.62  │  Augen (Kamera-Position)
Y + 0.00  ┴  Füße (Player.Position.Y)
```

Das bedeutet:
```csharp
// Kamera folgt dem Spieler:
camera.Position = player.Position + new Vector3(0, Player.EyeHeight, 0);
```

## Was sich in Engine.cs ändert

```csharp
// Vorher:
_camera.ProcessKeyboard(input, deltaTime);

// Nachher:
_player.ProcessInput(input, deltaTime);           // Spieler bewegt sich
_camera.Position = _player.EyePosition;          // Kamera folgt
_camera.ProcessMouseMovement(deltaX, deltaY);    // Blickrichtung bleibt in Kamera
```

Die Kamera behält Yaw/Pitch — sie schaut wohin der Spieler schaut. Aber die **Position** kommt jetzt vom Spieler.

## Fly-Modus

Aktuell fliegen wir frei. Den Fly-Modus behalten wir als Debug-Feature:

```csharp
public bool FlyMode { get; set; } = true;  // Default: fliegen bis Kollision fertig

// FlyMode = true:  WASD bewegt in Blickrichtung (aktuelles Verhalten)
// FlyMode = false: WASD bewegt horizontal, Gravitation zieht nach unten
```

Das `fly` Kommando in der Debug-Konsole schaltet zwischen beiden um.
