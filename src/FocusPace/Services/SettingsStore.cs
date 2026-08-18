using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FocusPace.Models;

namespace FocusPace.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsStore()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DirectoryPath = Path.Combine(localData, "FocusPace");
        FilePath = Path.Combine(DirectoryPath, "settings.json");
    }

    public string DirectoryPath { get; }
    public string FilePath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(AppSettings.ColorThemeVersion), out _))
            {
                // Ocean was the original implicit default. Move only that value to the new
                // multicolor brand default, while preserving any theme the user chose.
                if (settings.ColorTheme == ColorThemeKind.Ocean)
                {
                    settings.ColorTheme = ColorThemeKind.Brand;
                }

                settings.ColorThemeVersion = 1;
            }

            settings.FocusMinutes = Math.Clamp(settings.FocusMinutes, 1, 240);
            settings.RestMinutes = Math.Clamp(settings.RestMinutes, 1, 60);
            settings.WidgetOpacity = settings.WidgetOpacity switch
            {
                WidgetOpacityKind.Opacity95 => WidgetOpacityKind.Opacity90,
                WidgetOpacityKind.Opacity40 => WidgetOpacityKind.Opacity60,
                WidgetOpacityKind.Opacity20 => WidgetOpacityKind.Opacity60,
                WidgetOpacityKind.Opacity0 => WidgetOpacityKind.Opacity60,
                _ => settings.WidgetOpacity
            };
            settings.WidgetPlacement ??= new WidgetPlacement();
            return settings;
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var temporaryPath = FilePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, FilePath, true);
        }
        catch (IOException)
        {
            // Timing must continue even when settings cannot be written.
        }
        catch (UnauthorizedAccessException)
        {
            // A locked-down profile should not make the utility unusable.
        }
    }
}
