namespace FocusPace.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset BootMarkerUtc { get; }
    long MonotonicTimestamp { get; }
    TimeSpan GetElapsedTime(long startTimestamp);
}

public sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();

    private SystemClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset BootMarkerUtc => UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);

    public long MonotonicTimestamp => System.Diagnostics.Stopwatch.GetTimestamp();

    public TimeSpan GetElapsedTime(long startTimestamp) => System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
}
