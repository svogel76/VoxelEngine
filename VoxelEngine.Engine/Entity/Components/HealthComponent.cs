using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.Components;

public sealed class HealthComponent : IComponent
{
    public string ComponentId => "health";

    public float MaxHp      { get; }
    public float CurrentHp  { get; private set; }
    public bool  IsDead     => CurrentHp <= 0f;

    public HealthComponent(float maxHp)
    {
        if (maxHp <= 0f)
            throw new ArgumentOutOfRangeException(nameof(maxHp), "MaxHp must be greater than zero.");

        MaxHp     = maxHp;
        CurrentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        CurrentHp = MathF.Max(0f, CurrentHp - amount);
    }

    public void Regenerate(float amount, double deltaTime)
    {
        if (amount <= 0f || IsDead) return;
        CurrentHp = MathF.Min(MaxHp, CurrentHp + amount * (float)deltaTime);
    }

    public void RestoreHealth(float value)
    {
        CurrentHp = Math.Clamp(value, 0f, MaxHp);
    }

    public void Update(IEntity entity, IModContext context, double deltaTime)
    {
        // Regeneration is driven externally (game rules); no passive regen here.
    }

    public static HealthComponent FromJson(JsonElement config)
    {
        float maxHp = config.TryGetProperty("max_hp", out var p) ? p.GetSingle() : 8f;
        return new HealthComponent(maxHp);
    }
}
