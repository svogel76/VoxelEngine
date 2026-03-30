using System.Numerics;

namespace VoxelEngine.Entity;

/// <summary>
/// Abstrakte Basis für alle Spielwelt-Entitäten.
/// Enthält Position, Velocity und Vitalwerte.
///
/// KEIN Silk.NET, kein Rendering, kein Spawning — das kommt in Phase 7.
/// Nur was der Spieler aktuell braucht.
/// </summary>
public abstract class Entity
{
    public Vector3      Position  { get; protected set; }
    public Vector3      Velocity  { get; protected set; }
    public EntityVitals Vitals    { get; }

    protected Entity(Vector3 startPosition, VitalsConfig? vitalsConfig = null)
    {
        Position = startPosition;
        Vitals   = new EntityVitals(vitalsConfig);
    }
}
