## Step-Up Konzept

Step-up bedeutet: wenn der Spieler horizontal gegen einen Block läuft der maximal `StepHeight` (default 1.0 Block) hoch ist, wird er automatisch darauf gehoben — ohne Sprung. Die Kamerabewegung auf die neue Höhe wird dabei leicht geglättet, damit der Step nicht sofort/snappend wirkt.

```
Ohne Step-up:          Mit Step-up:
                       
Spieler → [Block]      Spieler → ↑[Block]
bleibt stecken         klettert automatisch rauf
```

Die Logik fügt sich sauber in die Achsen-Auflösung ein:

```
1. Bewege X → Kollision?
   → Versuche Step-up: hebe Y um StepHeight
   → Kollision jetzt frei? → Step-up erfolgreich
   → Sonst: X-Bewegung blockiert

2. Bewege Y (Gravitation)
3. Bewege Z → gleiche Step-up Logik wie X
```

Wichtig: Step-up nur wenn `IsOnGround` — kein Step-up in der Luft.
