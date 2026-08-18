using System.Runtime.InteropServices;

namespace FocusPace.Services;

public interface IUserActivityMonitor
{
    uint LastInputTick { get; }
    TimeSpan IdleTime { get; }
}

public sealed class UserActivityMonitor : IUserActivityMonitor
{
    public uint LastInputTick
    {
        get
        {
            var information = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
            return GetLastInputInfo(ref information) ? information.Time : 0;
        }
    }

    public TimeSpan IdleTime
    {
        get
        {
            var lastInput = LastInputTick;
            if (lastInput == 0)
            {
                return TimeSpan.Zero;
            }

            var elapsedMilliseconds = unchecked(GetTickCount() - lastInput);
            return TimeSpan.FromMilliseconds(elapsedMilliseconds);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo information);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();
}
