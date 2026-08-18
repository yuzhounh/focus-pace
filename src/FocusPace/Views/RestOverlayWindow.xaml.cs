using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FocusPace.ViewModels;
using Forms = System.Windows.Forms;

namespace FocusPace.Views;

public partial class RestOverlayWindow : Window
{
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpShowWindow = 0x0040;
    private readonly Forms.Screen _screen;
    private readonly bool _showControls;
    private bool _allowClose;

    public RestOverlayWindow(Forms.Screen screen, bool showControls, string? wallpaperPath, AppViewModel viewModel)
    {
        _screen = screen;
        _showControls = showControls;
        InitializeComponent();
        DataContext = viewModel;
        ActionPanel.Visibility = showControls ? Visibility.Visible : Visibility.Collapsed;
        ShowActivated = showControls;
        LoadWallpaper(wallpaperPath);
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
        Closing += OnClosing;
    }

    public event EventHandler? ExtendFocusRequested;
    public event EventHandler? LeaveFullScreenRequested;

    public void CloseOverlay()
    {
        _allowClose = true;
        Close();
    }

    private void LoadWallpaper(string? wallpaperPath)
    {
        if (wallpaperPath is null || string.IsNullOrWhiteSpace(wallpaperPath) || !File.Exists(wallpaperPath))
        {
            return;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(wallpaperPath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            WallpaperImage.Source = image;
        }
        catch
        {
            WallpaperImage.Source = null;
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var bounds = _screen.Bounds;
        SetWindowPos(handle, HwndTopmost, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SwpShowWindow);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_showControls)
        {
            Activate();
            Focus();
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_showControls || _allowClose)
        {
            return;
        }

        Dispatcher.BeginInvoke(Activate);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
    }

    private void ExtendFocusButton_OnClick(object sender, RoutedEventArgs e) =>
        ExtendFocusRequested?.Invoke(this, EventArgs.Empty);

    private void LeaveFullScreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        RestActionsPanel.Visibility = Visibility.Collapsed;
        LeaveConfirmationPanel.Visibility = Visibility.Visible;
    }

    private void CancelLeaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        LeaveConfirmationPanel.Visibility = Visibility.Collapsed;
        RestActionsPanel.Visibility = Visibility.Visible;
    }

    private void ConfirmLeaveButton_OnClick(object sender, RoutedEventArgs e) =>
        LeaveFullScreenRequested?.Invoke(this, EventArgs.Empty);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
