using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace FocusPace.Services;

public static class WallpaperService
{
    private const uint SpiGetDesktopWallpaper = 0x0073;

    public static string? GetCurrentWallpaperPath()
    {
        var buffer = new StringBuilder(32768);
        if (SystemParametersInfo(SpiGetDesktopWallpaper, (uint)buffer.Capacity, buffer, 0))
        {
            var path = buffer.ToString();
            if (File.Exists(path))
            {
                return path;
            }
        }

        try
        {
            var path = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WallPaper", null) as string;
            return File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint parameter, StringBuilder value, uint flags);
}
