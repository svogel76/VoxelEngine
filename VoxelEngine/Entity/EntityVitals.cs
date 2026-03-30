namespace VoxelEngine.Entity;

/// <summary>
/// Unveränderliche Konfiguration der Vitalwerte — wird beim Erstellen einer Entity
/// übergeben und definiert die Balance-Parameter.
/// </summary>
public sealed record VitalsConfig(
    /// <summary>Maximale Gesundheit.</summary>
    float MaxHealth          = 20f,

    /// <summary>Maximaler Hunger.</summary>
    float MaxHunger          = 20f,

    /// <summary>Hunger-Level ab dem Regeneration einsetzt.</summary>
    float RegenMinHunger     = 18f,

    /// <summary>HP pro Sekunde bei ausreichendem Hunger.</summary>
    float RegenRatePerSecond = 1f,

    /// <summary>Hunger-Level unter dem das Verhungern einsetzt.</summary>
    float StarvationThreshold = 0f,

    /// <summary>Schaden pro Sekunde beim Verhungern.</summary>
    float StarvationDamagePerSecond = 1f,

    /// <summary>Hunger-Verbrauch pro Sekunde bei Bewegung.</summary>
    float HungerDrainActive  = 0.025f,

    /// <summary>Hunger-Verbrauch pro Sekunde im Stillstand.</summary>
    float HungerDrainPassive = 0.005f,

    /// <summary>Minimale Fallhöhe in Einheiten ab der Schaden entsteht.</summary>
    float FallDamageMinHeight = 3f,

    /// <summary>Schaden pro Einheit Fallhöhe über dem Mindestgrenzwert.</summary>
    float FallDamagePerUnit  = 2f
);

/// <summary>
/// Veränderlicher Zustand der Entity-Vitalwerte.
/// Nur via <see cref="EntityVitals"/>-Methoden modifizieren — nie Felder direkt setzen.
/// </summary>
public sealed class EntityVitals
{
    private readonly VitalsConfig _cfg;

    public float Health  { get; private set; }
    public float Hunger  { get; private set; }
    public float MaxHealth => _cfg.MaxHealth;
    public float MaxHunger => _cfg.MaxHunger;

    /// <summary>True solange Health > 0.</summary>
    public bool IsAlive  => Health > 0f;

    /// <summary>Fallhöhe in Einheiten (zum Erkennen von Landeimpact-Moment setzen).</summary>
    public float FallDistance { get; set; }

    public EntityVitals(VitalsConfig? config = null)
    {
        _cfg   = config ?? new VitalsConfig();
        Health = _cfg.MaxHealth;
        Hunger = _cfg.MaxHunger;
    }

    // ── Direktmodifikation ────────────────────────────────────────────────

    /// <summary>Fügt Schaden zu. Klemmt auf [0, MaxHealth].</summary>
    public void Damage(float amount)
    {
        if (amount < 0f) return;
        Health = MathF.Max(0f, Health - amount);
    }

    /// <summary>Heilt. Klemmt auf MaxHealth.</summary>
    public void Heal(float amount)
    {
        if (amount < 0f) return;
        Health = MathF.Min(_cfg.MaxHealth, Health + amount);
    }

    /// <summary>Erhöht den Hunger-Wert (Essen). Klemmt auf MaxHunger.</summary>
    public void Feed(float amount)
    {
        if (amount < 0f) return;
        Hunger = MathF.Min(_cfg.MaxHunger, Hunger + amount);
    }

    /// <summary>Reduziert Hunger. Klemmt auf 0.</summary>
    public void DrainHunger(float amount)
    {
        if (amount < 0f) return;
        Hunger = MathF.Max(0f, Hunger - amount);
    }

    // ── Spiellogik-Tick ───────────────────────────────────────────────────

    /// <summary>
    /// Verarbeitet Hunger-Verbrauch, Regeneration und Verhungern für einen Zeitschritt.
    /// </summary>
    /// <param name="deltaTime">Zeitschritt in Sekunden.</param>
    /// <param name="isMoving">True wenn die Entity sich aktiv bewegt.</param>
    public void Tick(float deltaTime, bool isMoving)
    {
        if (!IsAlive) return;

        // Hunger ablaufen lassen
        float drain = isMoving ? _cfg.HungerDrainActive : _cfg.HungerDrainPassive;
        DrainHunger(drain * deltaTime);

        // Regeneration wenn Hunger ausreichend
        if (Hunger >= _cfg.RegenMinHunger && Health < _cfg.MaxHealth)
            Heal(_cfg.RegenRatePerSecond * deltaTime);

        // Verhungern wenn Hunger 0
        if (Hunger <= _cfg.StarvationThreshold)
            Damage(_cfg.StarvationDamagePerSecond * deltaTime);
    }

    /// <summary>
    /// Berechnet und wendet Fallschaden an.
    /// Muss genau einmal aufgerufen werden wenn die Entity landet.
    /// </summary>
    public void ApplyFallDamage()
    {
        if (!IsAlive) return;
        float excess = FallDistance - _cfg.FallDamageMinHeight;
        if (excess > 0f)
            Damage(excess * _cfg.FallDamagePerUnit);
        FallDistance = 0f;
    }

    /// <summary>Setzt alle Werte auf Maximum (z.B. nach Respawn).</summary>
    public void Reset()
    {
        Health = _cfg.MaxHealth;
        Hunger = _cfg.MaxHunger;
        FallDistance = 0f;
    }

    // ── Direkte Wertzuweisung für Serialisierung / Tests ──────────────────

    public void RestoreHealth(float value) => Health = Math.Clamp(value, 0f, _cfg.MaxHealth);
    public void RestoreHunger(float value) => Hunger = Math.Clamp(value, 0f, _cfg.MaxHunger);
}
