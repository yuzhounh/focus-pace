namespace FocusPace.Models;

public enum SessionPhase
{
    Ready,
    Focus,
    Rest
}

public enum WidgetStyleKind
{
    Bar,
    Ring,
    Fluid
}

public enum ColorThemeKind
{
    Brand,
    Ocean,
    Violet,
    Mint,
    Amber,
    Rose,
    Silver
}

public enum WidgetMotionKind
{
    Static,
    Dynamic
}

public enum WidgetOpacityKind
{
    Opacity100,
    Opacity90,
    Opacity80,
    Opacity70,
    Opacity60,
    // Kept for migration from builds that exposed other presets.
    Opacity95,
    Opacity40,
    Opacity20,
    Opacity0
}

public sealed class AppSettings
{
    public int FocusMinutes { get; set; } = 45;
    public int RestMinutes { get; set; } = 5;
    public bool StartWithWindows { get; set; }
    public bool WidgetAlwaysOnTop { get; set; } = true;
    public WidgetStyleKind WidgetStyle { get; set; } = WidgetStyleKind.Bar;
    public WidgetMotionKind WidgetMotion { get; set; } = WidgetMotionKind.Dynamic;
    public WidgetOpacityKind WidgetOpacity { get; set; } = WidgetOpacityKind.Opacity90;
    public ColorThemeKind ColorTheme { get; set; } = ColorThemeKind.Brand;
    public int ColorThemeVersion { get; set; } = 1;
    public WidgetPlacement WidgetPlacement { get; set; } = new();
    public SessionSnapshot? Session { get; set; }
}

public sealed class WidgetPlacement
{
    public bool HasValue { get; set; }
    public string? MonitorDeviceName { get; set; }
    public double RelativeX { get; set; }
    public double RelativeY { get; set; }
}

public sealed class SessionSnapshot
{
    public SessionPhase Phase { get; set; }
    public bool IsPaused { get; set; }
    public long TargetTicks { get; set; }
    public long AccumulatedTicks { get; set; }
    public DateTimeOffset? RunningSinceUtc { get; set; }
    public bool GoalAnnounced { get; set; }
    public DateTimeOffset BootMarkerUtc { get; set; }
}
