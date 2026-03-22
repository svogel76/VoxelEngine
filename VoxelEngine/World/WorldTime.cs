namespace VoxelEngine.World;

public class WorldTime
{
    public double Time      { get; private set; } = 8.0;
    public double TimeScale { get; set; }         = 72.0;
    public bool   Paused    { get; set; }         = false;

    public bool  IsDay           => Time >= 7.0 && Time < 19.0;
    public bool  IsNight         => !IsDay;
    public float NormalizedTime  => (float)(Time / 24.0);

    public void Update(double deltaTime)
    {
        if (!Paused)
            Time = (Time + deltaTime * TimeScale / 3600.0 * 24.0) % 24.0;
    }

    public void SetTime(double hours)
        => Time = Math.Clamp(hours, 0.0, 23.999);
}
