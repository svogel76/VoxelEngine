using VoxelEngine.Entity;
using Xunit;

namespace VoxelEngine.Tests.Entity;

public class EntityVitalsTests
{
    // ── Hilfsmethoden ─────────────────────────────────────────────────────

    private static EntityVitals Make(VitalsConfig? cfg = null)
        => new(cfg ?? new VitalsConfig());

    // Tick-Hilfsmethode für Präzision: n × 1s
    private static void TickSeconds(EntityVitals v, int seconds, bool isMoving = false)
    {
        for (int i = 0; i < seconds; i++)
            v.Tick(1f, isMoving);
    }

    // ── Initialzustand ────────────────────────────────────────────────────

    [Fact]
    public void InitialValues_AreMaximum()
    {
        var v = Make();
        Assert.Equal(20f, v.Health);
        Assert.Equal(20f, v.Hunger);
        Assert.True(v.IsAlive);
    }

    [Fact]
    public void IsAlive_FalseWhenHealthZero()
    {
        var v = Make();
        v.Damage(20f);
        Assert.False(v.IsAlive);
    }

    // ── Direktmodifikation ────────────────────────────────────────────────

    [Fact]
    public void Damage_ReducesHealth()
    {
        var v = Make();
        v.Damage(5f);
        Assert.Equal(15f, v.Health);
    }

    [Fact]
    public void Damage_ClampsAtZero()
    {
        var v = Make();
        v.Damage(99f);
        Assert.Equal(0f, v.Health);
    }

    [Fact]
    public void Heal_IncreasesHealth()
    {
        var v = Make();
        v.Damage(10f);
        v.Heal(3f);
        Assert.Equal(13f, v.Health);
    }

    [Fact]
    public void Heal_ClampsAtMaxHealth()
    {
        var v = Make();
        v.Heal(99f);
        Assert.Equal(20f, v.Health);
    }

    [Fact]
    public void Feed_IncreasesHunger()
    {
        var v = Make();
        v.DrainHunger(10f);
        v.Feed(5f);
        Assert.Equal(15f, v.Hunger);
    }

    [Fact]
    public void Feed_ClampsAtMaxHunger()
    {
        var v = Make();
        v.Feed(99f);
        Assert.Equal(20f, v.Hunger);
    }

    [Fact]
    public void DrainHunger_ClampsAtZero()
    {
        var v = Make();
        v.DrainHunger(99f);
        Assert.Equal(0f, v.Hunger);
    }

    [Fact]
    public void Damage_Negative_IsIgnored()
    {
        var v = Make();
        v.Damage(-5f);
        Assert.Equal(20f, v.Health);
    }

    [Fact]
    public void Heal_Negative_IsIgnored()
    {
        var v = Make();
        v.Heal(-5f);
        Assert.Equal(20f, v.Health);
    }

    // ── Hunger-Drain per Tick ─────────────────────────────────────────────

    [Fact]
    public void Tick_DrainHunger_Passive()
    {
        // Standardkonfiguration: passiver Drain = 0.005f/s
        var cfg = new VitalsConfig(HungerDrainPassive: 0.005f, HungerDrainActive: 0.025f,
                                    RegenMinHunger: 18f, RegenRatePerSecond: 0f,
                                    StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.Tick(1f, isMoving: false);
        Assert.Equal(20f - 0.005f, v.Hunger, precision: 4);
    }

    [Fact]
    public void Tick_DrainHunger_Active_IsHigher()
    {
        var cfg = new VitalsConfig(HungerDrainPassive: 0.005f, HungerDrainActive: 0.025f,
                                    RegenMinHunger: 18f, RegenRatePerSecond: 0f,
                                    StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.Tick(1f, isMoving: true);
        Assert.Equal(20f - 0.025f, v.Hunger, precision: 4);
    }

    // ── Regeneration ──────────────────────────────────────────────────────

    [Fact]
    public void Tick_Regen_OnlyWhenHungerSufficient()
    {
        // Hunger unter Schwellwert → keine Regen
        var cfg = new VitalsConfig(RegenMinHunger: 18f, RegenRatePerSecond: 2f,
                                    HungerDrainPassive: 0f, HungerDrainActive: 0f,
                                    StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.DrainHunger(5f);   // Hunger = 15 < RegenMin 18 → kein Regen
        v.Damage(5f);        // Health = 15
        v.Tick(1f, false);
        Assert.Equal(15f, v.Health);
    }

    [Fact]
    public void Tick_Regen_WhenHungerSufficient()
    {
        var cfg = new VitalsConfig(RegenMinHunger: 18f, RegenRatePerSecond: 2f,
                                    HungerDrainPassive: 0f, HungerDrainActive: 0f,
                                    StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.Damage(4f);        // Health = 16, Hunger = 20 ≥ 18 → Regen aktiv
        v.Tick(1f, false);
        Assert.Equal(18f, v.Health, precision: 4);
    }

    [Fact]
    public void Tick_Regen_DoesNotExceedMaxHealth()
    {
        var cfg = new VitalsConfig(RegenMinHunger: 0f, RegenRatePerSecond: 100f,
                                    HungerDrainPassive: 0f, HungerDrainActive: 0f,
                                    StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.Damage(1f);
        v.Tick(1f, false);
        Assert.Equal(20f, v.Health);
    }

    [Fact]
    public void Tick_NoRegen_WhenHealthFull()
    {
        var cfg = new VitalsConfig(RegenMinHunger: 0f, RegenRatePerSecond: 5f,
                                    HungerDrainPassive: 0f, HungerDrainActive: 0f,
                                    StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        // Health ist voll → Regen darf nicht auslösen
        v.Tick(1f, false);
        Assert.Equal(20f, v.Health);
    }

    // ── Verhungern ────────────────────────────────────────────────────────

    [Fact]
    public void Tick_Starvation_WhenHungerZero()
    {
        var cfg = new VitalsConfig(
            StarvationThreshold: 0f,
            StarvationDamagePerSecond: 1f,
            HungerDrainPassive: 0f, HungerDrainActive: 0f,
            RegenMinHunger: 18f, RegenRatePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.DrainHunger(20f);   // Hunger = 0
        v.Tick(1f, false);
        Assert.Equal(19f, v.Health, precision: 4);
    }

    [Fact]
    public void Tick_Starvation_NotWhenHungerAboveThreshold()
    {
        var cfg = new VitalsConfig(
            StarvationThreshold: 0f,
            StarvationDamagePerSecond: 1f,
            HungerDrainPassive: 0f, HungerDrainActive: 0f,
            RegenMinHunger: 999f, RegenRatePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.DrainHunger(10f);   // Hunger = 10 > Threshold 0
        v.Tick(1f, false);
        Assert.Equal(20f, v.Health);
    }

    [Fact]
    public void Tick_Starvation_CanKill()
    {
        var cfg = new VitalsConfig(
            StarvationThreshold: 0f,
            StarvationDamagePerSecond: 20f,
            HungerDrainPassive: 0f, HungerDrainActive: 0f,
            RegenMinHunger: 999f, RegenRatePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.DrainHunger(20f);   // Hunger = 0
        v.Tick(2f, false);    // 40 Schaden → tot
        Assert.False(v.IsAlive);
        Assert.Equal(0f, v.Health);
    }

    [Fact]
    public void Tick_NoTick_WhenDead()
    {
        var cfg = new VitalsConfig(
            HungerDrainPassive: 0.1f,
            RegenRatePerSecond: 0f,
            StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.Damage(20f);   // Tot
        float hungerBefore = v.Hunger;
        v.Tick(10f, false);
        Assert.Equal(hungerBefore, v.Hunger);   // Keine Veränderung
    }

    // ── Fallschaden ───────────────────────────────────────────────────────

    [Fact]
    public void FallDamage_BelowMinHeight_NoDamage()
    {
        var cfg = new VitalsConfig(FallDamageMinHeight: 3f, FallDamagePerUnit: 2f);
        var v   = new EntityVitals(cfg);
        v.FallDistance = 2.9f;
        v.ApplyFallDamage();
        Assert.Equal(20f, v.Health);
    }

    [Fact]
    public void FallDamage_ExactlyAtMinHeight_NoDamage()
    {
        var cfg = new VitalsConfig(FallDamageMinHeight: 3f, FallDamagePerUnit: 2f);
        var v   = new EntityVitals(cfg);
        v.FallDistance = 3f;
        v.ApplyFallDamage();
        Assert.Equal(20f, v.Health);
    }

    [Fact]
    public void FallDamage_AboveMinHeight_DeductsDamage()
    {
        var cfg = new VitalsConfig(FallDamageMinHeight: 3f, FallDamagePerUnit: 2f);
        var v   = new EntityVitals(cfg);
        v.FallDistance = 5f;   // Überschuss: 2 Einheiten × 2 HP = 4 Schaden
        v.ApplyFallDamage();
        Assert.Equal(16f, v.Health, precision: 4);
    }

    [Fact]
    public void FallDamage_ResetsDistanceAfterApply()
    {
        var cfg = new VitalsConfig(FallDamageMinHeight: 3f, FallDamagePerUnit: 2f);
        var v   = new EntityVitals(cfg);
        v.FallDistance = 10f;
        v.ApplyFallDamage();
        Assert.Equal(0f, v.FallDistance);
    }

    [Fact]
    public void FallDamage_CanKill()
    {
        var cfg = new VitalsConfig(FallDamageMinHeight: 0f, FallDamagePerUnit: 3f);
        var v   = new EntityVitals(cfg);
        v.FallDistance = 10f;   // 10 × 3 = 30 Schaden → tot
        v.ApplyFallDamage();
        Assert.False(v.IsAlive);
        Assert.Equal(0f, v.Health);
    }

    [Fact]
    public void FallDamage_NotApplied_WhenDead()
    {
        var cfg = new VitalsConfig(FallDamageMinHeight: 0f, FallDamagePerUnit: 10f);
        var v   = new EntityVitals(cfg);
        v.Damage(20f);   // Tot
        v.FallDistance = 5f;
        v.ApplyFallDamage();
        Assert.Equal(0f, v.Health);   // Bleibt bei 0, kein negativer Wert
    }

    // ── Reset ─────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_RestoresAllValuesToMax()
    {
        var v = Make();
        v.Damage(10f);
        v.DrainHunger(10f);
        v.FallDistance = 5f;
        v.Reset();
        Assert.Equal(20f, v.Health);
        Assert.Equal(20f, v.Hunger);
        Assert.Equal(0f,  v.FallDistance);
    }

    // ── Restore (Serialisierung) ──────────────────────────────────────────

    [Fact]
    public void RestoreHealth_ClampsToRange()
    {
        var v = Make();
        v.RestoreHealth(99f);
        Assert.Equal(20f, v.Health);

        v.RestoreHealth(-5f);
        Assert.Equal(0f, v.Health);
    }

    [Fact]
    public void RestoreHunger_ClampsToRange()
    {
        var v = Make();
        v.RestoreHunger(99f);
        Assert.Equal(20f, v.Hunger);

        v.RestoreHunger(-5f);
        Assert.Equal(0f, v.Hunger);
    }

    // ── Kombiniert: Hunger → Regen-Interaktion ────────────────────────────

    [Fact]
    public void Regen_Activates_AfterEating()
    {
        var cfg = new VitalsConfig(
            RegenMinHunger: 18f,
            RegenRatePerSecond: 4f,
            HungerDrainPassive: 0f, HungerDrainActive: 0f,
            StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        v.DrainHunger(5f);   // Hunger = 15 → kein Regen
        v.Damage(8f);        // Health = 12
        v.Tick(1f, false);
        Assert.Equal(12f, v.Health);   // noch kein Regen

        v.Feed(5f);           // Hunger = 20 ≥ 18 → Regen aktiv
        v.Tick(1f, false);
        Assert.Equal(16f, v.Health, precision: 4);
    }

    [Fact]
    public void HungerDrain_Over_Many_Ticks_AccumulatesCorrectly()
    {
        var cfg = new VitalsConfig(
            HungerDrainPassive: 0.005f,
            HungerDrainActive: 0.025f,
            RegenMinHunger: 999f,   // kein Regen
            RegenRatePerSecond: 0f,
            StarvationDamagePerSecond: 0f);
        var v = new EntityVitals(cfg);
        // 200 Sekunden passiv → drain = 200 × 0.005 = 1.0
        TickSeconds(v, 200, isMoving: false);
        Assert.Equal(19f, v.Hunger, precision: 2);
    }
}
