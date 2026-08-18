namespace FocusPace.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset BootMarkerUtc { get; }
}

public sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();

    private SystemClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset BootMarkerUtc => UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);
}

