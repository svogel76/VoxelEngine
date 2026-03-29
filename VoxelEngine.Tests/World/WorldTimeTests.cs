using FluentAssertions;
using VoxelEngine.World;
using Xunit;

namespace VoxelEngine.Tests.World;

public class WorldTimeTests
{
    // WorldTime.Update formula: deltaTime * TimeScale / 3600.0 * 24.0
    // Default TimeScale = 72.0 → 0.48 simulated hours per real second

    [Fact]
    public void Advance_IncreasesTime()
    {
        // Arrange
        var worldTime = new WorldTime();
        double startTime = worldTime.Time;

        // Act
        worldTime.Update(1.0); // 1 real second

        // Assert
        worldTime.Time.Should().BeGreaterThan(startTime);
    }

    [Fact]
    public void Time_WrapsAt24()
    {
        // Arrange
        var worldTime = new WorldTime();
        worldTime.SetTime(23.5);

        // Act – 2 real seconds at default TimeScale=72 advance 0.96 h → crosses 24.0
        worldTime.Update(2.0);

        // Assert
        worldTime.Time.Should().BeInRange(0.0, 24.0, because: "time must always be within a 24-hour cycle");
        worldTime.Time.Should().BeLessThan(1.0, because: "wrap should have occurred and only a fraction of an hour passed beyond midnight");
        worldTime.DayCount.Should().Be(1);
    }

    [Fact]
    public void MoonPhase_ChangesWithDayCount()
    {
        // Arrange
        var worldTime = new WorldTime();
        worldTime.SetTime(8.0);
        int initialDayCount = worldTime.DayCount; // 0

        // Act – advance exactly one full day:
        // need 16+ hours; 17 h / 0.48 h/s ≈ 35.42 s
        double deltaToAdvanceOneDay = 17.0 / (worldTime.TimeScale / 3600.0 * 24.0);
        worldTime.Update(deltaToAdvanceOneDay);

        // Assert
        worldTime.DayCount.Should().Be(initialDayCount + 1);
        worldTime.MoonPhase.Should().Be((initialDayCount + 1) % 8);
    }

    [Fact]
    public void MoonPhase_CyclesEvery8Days()
    {
        // Arrange
        var worldTime = new WorldTime();
        worldTime.SetTime(0.0);
        double hoursPerDay = 24.0;
        double secondsPerDay = hoursPerDay / (worldTime.TimeScale / 3600.0 * 24.0);

        // Act – advance exactly 8 days
        for (int day = 0; day < 8; day++)
            worldTime.Update(secondsPerDay);

        // Assert
        worldTime.DayCount.Should().Be(8);
        worldTime.MoonPhase.Should().Be(0, because: "moon phase completes a full cycle every 8 days");
    }

    [Fact]
    public void TimeScale_AffectsAdvanceRate()
    {
        // Arrange
        var slow = new WorldTime();
        slow.TimeScale = 72.0; // default

        var fast = new WorldTime();
        fast.TimeScale = 144.0; // 2× faster

        // Act
        slow.Update(1.0);
        fast.Update(1.0);

        // Assert
        fast.Time.Should().BeGreaterThan(slow.Time, because: "a higher TimeScale advances simulation time faster");
    }

    [Fact]
    public void Update_WhenPaused_DoesNotChangeTime()
    {
        // Arrange
        var worldTime = new WorldTime();
        worldTime.Paused = true;
        double timeBeforePause = worldTime.Time;

        // Act
        worldTime.Update(100.0);

        // Assert
        worldTime.Time.Should().Be(timeBeforePause);
    }
}
