using System.Windows;
using System.Windows.Media;
using FocusPace.Models;
using Microsoft.Win32;

namespace FocusPace.Services;

public static class ThemeService
{
    private static readonly string[] BrandColors =
    [
        "#668DD8", // Ocean
        "#8875DE", // Violet
        "#D06F91", // Rose
        "#D39448", // Amber
        "#43A68F"  // Mint
    ];

    private sealed record Palette(
        string Accent,
        string AccentHover,
        string Early,
        string Mid,
        string Late,
        string Rest,
        string Complete,
        string LightWindow,
        string DarkWindow,
        string LightWidget,
        string DarkWidget);

    private static readonly IReadOnlyDictionary<ColorThemeKind, Palette> Palettes =
        new Dictionary<ColorThemeKind, Palette>
        {
            [ColorThemeKind.Brand] = new("#668DD8", "#587FCC", "#668DD8", "#8875DE", "#D06F91", "#43A68F", "#D39448", "#F5F7FB", "#11151C", "#FFF6F8FC", "#FF18202A"),
            [ColorThemeKind.Ocean] = new("#668DD8", "#587FCC", "#6F86A8", "#648FD5", "#8A79D6", "#55A99A", "#61AF83", "#F5F7FB", "#11151C", "#FFF6F8FC", "#FF18202A"),
            [ColorThemeKind.Violet] = new("#8875DE", "#7864D0", "#766FA5", "#8D78DA", "#B26BC8", "#6DA7B8", "#68B88C", "#F8F6FC", "#16131E", "#FFF8F5FC", "#FF1F1A2A"),
            [ColorThemeKind.Mint] = new("#43A68F", "#359681", "#668F88", "#43A991", "#66BFA8", "#42A897", "#54B878", "#F3F9F7", "#101A18", "#FFF2FAF7", "#FF172622"),
            [ColorThemeKind.Amber] = new("#D39448", "#BF8036", "#9A856B", "#D39A53", "#D9795F", "#69A19A", "#66AD78", "#FBF8F2", "#1C1710", "#FFFCF8F0", "#FF2A2117"),
            [ColorThemeKind.Rose] = new("#D06F91", "#BF5D80", "#9B7283", "#D37597", "#B875C5", "#6EA2A2", "#66AD7D", "#FCF5F7", "#1D1217", "#FFFCF5F8", "#FF2B1921"),
            [ColorThemeKind.Silver] = new("#8B96A5", "#788391", "#A8B0BA", "#929CA9", "#7C8795", "#98A2AE", "#858F9C", "#F6F7F8", "#14171B", "#FFF7F8FA", "#FF1D2126")
        };

    public static void Apply(ResourceDictionary resources, ColorThemeKind colorTheme)
    {
        var dark = IsDarkMode();
        var palette = Palettes[colorTheme];
        resources["WindowBackgroundBrush"] = Brush(dark ? palette.DarkWindow : palette.LightWindow);
        resources["SurfaceBrush"] = Brush(dark ? "#F21B222D" : "#F2FFFFFF");
        resources["WidgetSurfaceBrush"] = Brush(dark ? palette.DarkWidget : palette.LightWidget);
        resources["ElevatedSurfaceBrush"] = Brush(dark ? "#FF222A36" : "#FFFFFFFF");
        resources["InputSurfaceBrush"] = Brush(dark ? "#FF252D39" : "#FFF0F3F8");
        resources["TextPrimaryBrush"] = Brush(dark ? "#FFF5F7FA" : "#FF172033");
        resources["TextSecondaryBrush"] = Brush(dark ? "#FFAAB4C3" : "#FF667085");
        resources["WidgetTextBrush"] = Brush(dark ? "#FFF7F9FC" : "#FF1D2939");
        resources["BorderBrush"] = Brush(dark ? "#33FFFFFF" : "#1F172033");
        resources["SecondaryHoverBrush"] = Brush(dark ? "#FF303947" : "#FFDCE3ED");
        if (colorTheme == ColorThemeKind.Brand)
        {
            resources["AccentBrush"] = BrandGradient();
            resources["AccentHoverBrush"] = BrandGradient(0.88);
            resources["ProgressEarlyBrush"] = BrandGradient();
            resources["ProgressMidBrush"] = BrandGradient();
            resources["ProgressLateBrush"] = BrandGradient();
            resources["ProgressRestBrush"] = BrandGradient();
            resources["ProgressCompleteBrush"] = BrandGradient();
        }
        else
        {
            resources["AccentBrush"] = Brush(palette.Accent);
            resources["AccentHoverBrush"] = Brush(palette.AccentHover);
            resources["ProgressEarlyBrush"] = Brush(palette.Early);
            resources["ProgressMidBrush"] = Brush(palette.Mid);
            resources["ProgressLateBrush"] = Brush(palette.Late);
            resources["ProgressRestBrush"] = Brush(palette.Rest);
            resources["ProgressCompleteBrush"] = Brush(palette.Complete);
        }

        resources["ProgressPausedBrush"] = Brush("#7D8491");
    }

    public static System.Windows.Media.Color GetAccentColor(ColorThemeKind colorTheme) =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Palettes[colorTheme].Accent);

    public static IReadOnlyList<System.Windows.Media.Color> GetBrandColors() =>
        BrandColors.Select(ParseColor).ToArray();

    private static bool IsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static LinearGradientBrush BrandGradient(double brightness = 1)
    {
        var gradient = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0.5),
            EndPoint = new System.Windows.Point(1, 0.5)
        };
        for (var index = 0; index < BrandColors.Length; index++)
        {
            var color = ParseColor(BrandColors[index]);
            if (brightness < 1)
            {
                color = System.Windows.Media.Color.FromArgb(
                    color.A,
                    (byte)Math.Round(color.R * brightness),
                    (byte)Math.Round(color.G * brightness),
                    (byte)Math.Round(color.B * brightness));
            }

            gradient.GradientStops.Add(new GradientStop(color, (double)index / (BrandColors.Length - 1)));
        }

        gradient.Freeze();
        return gradient;
    }

    private static System.Windows.Media.Color ParseColor(string color) =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
}
