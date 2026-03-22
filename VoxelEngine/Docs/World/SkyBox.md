# Konzept: Skybox
Eine Skybox ist ein riesiger Würfel der um die Kamera herum gerendert wird — immer an derselben Position wie die Kamera, sodass sie nie erreichbar wirkt.
```
Normale Objekte:    bewegen sich relativ zur Kamera
Skybox:             bewegt sich MIT der Kamera
                    → wirkt unendlich weit entfernt
```
Es gibt zwei klassische Ansätze:
Ansatz A: Cubemap Textur — 6 Texturseiten für alle Himmelsrichtungen. Realistisch, aber braucht externe Texturen.
Ansatz B: Prozeduraler Himmel — Farbe wird im Shader berechnet. Kein externes Asset nötig, sehr flexibel, perfekt für Tag/Nacht-Zyklus später.
Für unser Projekt ist Ansatz B die bessere Wahl — wir haben bereits keine externen Texturen und wollen später einen Tag/Nacht-Zyklus. Der prozedurale Ansatz zahlt sich dann doppelt aus.

## Das Prinzip: Gradient nach Blickrichtung
```
Blick nach oben   → Dunkelblau  (#1A6BA0)
Blick horizontal  → Hellblau    (#87CEEB)
Blick zum Horizont → Weißlich   (#C8E8F8)
```
Im Shader berechnen wir die Höhe des Blickvektors:
```glsl
float horizon = normalize(TexCoord).y; // -1 bis +1
vec3 skyColor = mix(horizonColor, zenithColor, max(horizon, 0.0));
vec3 groundColor = mix(groundColor, horizonColor, max(-horizon, 0.0));
FragColor = vec4(horizon > 0 ? skyColor : groundColor, 1.0);
```
Warum die View-Matrix ohne Translation
Das ist der Trick damit die Skybox "unendlich weit" wirkt:
```csharp
// Normale View-Matrix: enthält Kamera-Position
// → Skybox würde sich mit Kamera bewegen aber Größe wäre sichtbar

// Skybox View-Matrix: Translation entfernt
Matrix4X4<float> skyView = camera.ViewMatrix;
skyView.M41 = 0; // Translation X = 0
skyView.M42 = 0; // Translation Y = 0
skyView.M43 = 0; // Translation Z = 0
// → Nur Rotation bleibt → Skybox dreht sich mit Kamera
//   aber bewegt sich nie
```

## Rendering-Reihenfolge
```
1. Depth-Test deaktivieren
2. Skybox rendern (immer im Hintergrund)
3. Depth-Test aktivieren
4. Terrain rendern (überdeckt Skybox)
```
Und ein wichtiger Detail: die Skybox muss mit gl.DepthMask(false) gerendert werden damit sie nie in den Depth-Buffer schreibt — sonst blockiert sie das Terrain-Rendering.