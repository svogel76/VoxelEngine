namespace VoxelEngine.World;

public class WorldTime
{
    public double Time      { get; private set; } = 8.0;
    public double TimeScale { get; set; }         = 72.0;
    public bool   Paused    { get; set; }         = false;
    public int    DayCount  { get; private set; } = 0;

    public bool  IsDay           => Time >= 7.0 && Time < 19.0;
    public bool  IsNight         => !IsDay;
    public float NormalizedTime  => (float)(Time / 24.0);

    // 0 = Neumond, 2 = Halbmond, 4 = Vollmond, 6 = Halbmond abnehmend
    public int MoonPhase => DayCount % 8;

    public void Update(double deltaTime)
    {
        if (!Paused)
        {
            double newTime = Time + deltaTime * TimeScale / 3600.0 * 24.0;
            if (newTime >= 24.0) DayCount++;
            Time = newTime % 24.0;
        }
    }

    public void SetTime(double hours)
        => Time = Math.Clamp(hours, 0.0, 23.999);

    /// <summary>
    /// Stellt Uhrzeit und Tagzähler aus einem gespeicherten Spielstand wieder her.
    /// </summary>
    public void Restore(double time, int dayCount)
    {
        Time     = Math.Clamp(time, 0.0, 23.999);
        DayCount = Math.Max(0, dayCount);
    }
}
