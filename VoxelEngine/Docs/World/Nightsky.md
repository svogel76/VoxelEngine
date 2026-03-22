# Konzept: Sterne
Sterne sind überraschend einfach zu implementieren — und der visuelle Effekt ist groß. Der Nachthimmel wirkt aktuell noch leer.

## Der Ansatz: Instanced Billboard Quads
Statt 1000 einzelne Draw Calls (einen pro Stern) nutzen wir Instancing — alle Sterne werden in einem einzigen Draw Call gerendert. Jede Instanz hat eine zufällige Position auf der Himmelskugel und eine zufällige Größe.
```
1 Draw Call → 1000 Sterne
vs.
1000 Draw Calls → 1000 Sterne  ← viel zu teuer
```
## Die Verteilung auf der Himmelskugel
Sterne müssen gleichmäßig auf einer Kugel verteilt werden — nicht in einem Würfel:
```csharp
// Gleichmäßige Kugelverteilung (Marsaglia-Methode)
float theta = Random.NextSingle() * 2 * MathF.PI;
float phi   = MathF.Acos(2 * Random.NextSingle() - 1);
float x = MathF.Sin(phi) * MathF.Cos(theta);
float y = MathF.Sin(phi) * MathF.Sin(theta);
float z = MathF.Cos(phi);
// Nur obere Hälfte: if (y < 0) y = -y;
```
## Sichtbarkeit mit Tageszeit
Sterne verschwinden tagsüber und erscheinen nachts — sanft interpoliert:
```csharp
// Sterne sichtbar wenn Sonne tief steht
float sunHeight = MathF.Sin(sunAngle * MathF.PI / 180f);
float starOpacity = Math.Clamp(-sunHeight * 3f, 0f, 1f);
// sunHeight < 0 (Nacht) → Sterne sichtbar
// sunHeight > 0 (Tag)   → Sterne unsichtbar
```
## Twinkle-Effekt
Ein leichtes Flackern macht Sterne realistischer — einfach mit einer Sinus-Funktion pro Stern:
```glsl
// Im Fragment Shader:
float twinkle = 0.85 + 0.15 * sin(uTime * starSpeed + starPhase);
FragColor = vec4(starColor * twinkle, opacity);
```