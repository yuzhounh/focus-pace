using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using FocusPace.Core;
using FocusPace.Models;
using FocusPace.ViewModels;
using Forms = System.Windows.Forms;

namespace FocusPace.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly AppViewModel _viewModel;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly ContextMenu _menu;
    private readonly MenuItem _statusItem;
    private readonly MenuItem _pauseItem;
    private readonly MenuItem _restartItem;
    private readonly MenuItem _focusItem;
    private readonly MenuItem _restItem;
    private readonly MenuItem _widgetItem;
    private readonly MenuItem _startupItem;
    private readonly MenuItem _alwaysOnTopItem;
    private readonly MenuItem _voiceAnnouncementsItem;
    private readonly IReadOnlyDictionary<WidgetStyleKind, MenuItem> _widgetStyleItems;
    private readonly IReadOnlyDictionary<WidgetMotionKind, MenuItem> _widgetMotionItems;
    private readonly IReadOnlyDictionary<WidgetOpacityKind, MenuItem> _widgetOpacityItems;
    private readonly IReadOnlyDictionary<ColorThemeKind, MenuItem> _colorThemeItems;
    private Icon _icon;

    public TrayIconService(AppViewModel viewModel)
    {
        _viewModel = viewModel;
        _icon = CreateProgressIcon();
        _statusItem = CreateMenuItem(string.Empty, null, false);
        _pauseItem = CreateMenuItem(string.Empty, () => _viewModel.PauseResumeCommand.Execute(null));
        _restartItem = CreateMenuItem("Restart session", () => _viewModel.RestartCommand.Execute(null));
        _focusItem = CreateMenuItem("Start Focus", () => _viewModel.StartFocusCommand.Execute(null));
        _restItem = CreateMenuItem("Start Rest", () => _viewModel.StartRestCommand.Execute(null));
        _widgetItem = CreateMenuItem(string.Empty, () => _viewModel.ToggleWidgetCommand.Execute(null));
        _startupItem = CreateMenuItem("Start with Windows", () => _viewModel.StartWithWindows = !_viewModel.StartWithWindows);
        _startupItem.IsCheckable = true;
        _alwaysOnTopItem = CreateMenuItem("Always on top", () => _viewModel.WidgetAlwaysOnTop = !_viewModel.WidgetAlwaysOnTop);
        _alwaysOnTopItem.IsCheckable = true;
        _voiceAnnouncementsItem = CreateMenuItem("Voice announcements", () =>
            _viewModel.VoiceAnnouncementsEnabled = !_viewModel.VoiceAnnouncementsEnabled);
        _voiceAnnouncementsItem.IsCheckable = true;
        _widgetStyleItems = Enum.GetValues<WidgetStyleKind>().ToDictionary(
            style => style,
            style => CreateCheckMenuItem(style.ToString(), () => _viewModel.SelectedWidgetStyle = style));
        _widgetMotionItems = Enum.GetValues<WidgetMotionKind>().ToDictionary(
            motion => motion,
            motion => CreateCheckMenuItem(motion.ToString(), () => _viewModel.SelectedWidgetMotion = motion));
        WidgetOpacityKind[] opacityPresets =
        [
            WidgetOpacityKind.Opacity100,
            WidgetOpacityKind.Opacity90,
            WidgetOpacityKind.Opacity80,
            WidgetOpacityKind.Opacity70,
            WidgetOpacityKind.Opacity60
        ];
        _widgetOpacityItems = opacityPresets.ToDictionary(
            opacity => opacity,
            opacity => CreateCheckMenuItem(OpacityLabel(opacity), () => _viewModel.SelectedWidgetOpacity = opacity));
        _colorThemeItems = Enum.GetValues<ColorThemeKind>().ToDictionary(
            colorTheme => colorTheme,
            colorTheme => CreateCheckMenuItem(colorTheme.ToString(), () => _viewModel.SelectedColorTheme = colorTheme));

        var widgetStyleMenu = CreateMenuItem("Widget style", null);
        foreach (var item in _widgetStyleItems.Values)
        {
            widgetStyleMenu.Items.Add(item);
        }

        var widgetMotionMenu = CreateMenuItem("Widget motion", null);
        foreach (var item in _widgetMotionItems.OrderByDescending(pair => pair.Key).Select(pair => pair.Value))
        {
            widgetMotionMenu.Items.Add(item);
        }

        var widgetOpacityMenu = CreateMenuItem("Widget opacity", null);
        foreach (var item in _widgetOpacityItems.Values)
        {
            widgetOpacityMenu.Items.Add(item);
        }

        var colorMenu = CreateMenuItem("Color", null);
        foreach (var item in _colorThemeItems.Values)
        {
            colorMenu.Items.Add(item);
        }

        _menu = new ContextMenu
        {
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = -190,
            StaysOpen = false
        };
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(CreateMenuItem("Open Focus Pace", () => _viewModel.ShowSettingsCommand.Execute(null)));
        _menu.Items.Add(new Separator());
        _menu.Items.Add(_startupItem);
        _menu.Items.Add(_alwaysOnTopItem);
        _menu.Items.Add(_voiceAnnouncementsItem);
        _menu.Items.Add(widgetStyleMenu);
        _menu.Items.Add(widgetMotionMenu);
        _menu.Items.Add(widgetOpacityMenu);
        _menu.Items.Add(colorMenu);
        _menu.Items.Add(new Separator());
        _menu.Items.Add(_pauseItem);
        _menu.Items.Add(_restartItem);
        _menu.Items.Add(_focusItem);
        _menu.Items.Add(_restItem);
        _menu.Items.Add(new Separator());
        _menu.Items.Add(_widgetItem);
        _menu.Items.Add(new Separator());
        _menu.Items.Add(CreateMenuItem("Exit", () => _viewModel.ExitCommand.Execute(null)));
        _menu.Opened += MenuOnOpened;

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Focus Pace",
            Icon = _icon,
            Visible = true
        };
        _notifyIcon.MouseUp += NotifyIconOnMouseUp;
        _notifyIcon.DoubleClick += (_, _) => _viewModel.ShowSettingsCommand.Execute(null);
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        _viewModel.AppearanceChanged += ViewModelOnAppearanceChanged;
        RefreshMenu();
    }

    public void Dispose()
    {
        _viewModel.AppearanceChanged -= ViewModelOnAppearanceChanged;
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        _menu.Opened -= MenuOnOpened;
        _menu.IsOpen = false;
        _notifyIcon.MouseUp -= NotifyIconOnMouseUp;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private void NotifyIconOnMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _menu.IsOpen = false;
                _viewModel.ShowSettingsCommand.Execute(null);
            });
            return;
        }

        if (e.Button != Forms.MouseButtons.Right)
        {
            return;
        }

        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RefreshMenu();
            _menu.IsOpen = true;
        });
    }

    private void MenuOnOpened(object sender, RoutedEventArgs e)
    {
        RefreshMenu();
        System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (PresentationSource.FromVisual(_menu) is not HwndSource source)
            {
                return;
            }

            SetForegroundWindow(source.Handle);
            _menu.Focus();
        });
    }

    private void ViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppViewModel.WidgetText) or nameof(AppViewModel.TimeText))
        {
            _notifyIcon.Text = Truncate($"Focus Pace · {_viewModel.WidgetText}", 63);
        }
    }

    private void ViewModelOnAppearanceChanged(object? sender, EventArgs e)
    {
        var replacement = CreateProgressIcon();
        _notifyIcon.Icon = replacement;
        var previous = _icon;
        _icon = replacement;
        previous.Dispose();
        RefreshMenu();
    }

    private void RefreshMenu()
    {
        _statusItem.Header = _viewModel.IsReady ? "Ready · Focus" : $"{_viewModel.PhaseLabel} · {_viewModel.TimeText}";
        _pauseItem.Header = _viewModel.PauseResumeText;
        _pauseItem.IsEnabled = _viewModel.HasActiveSession;
        _restartItem.IsEnabled = _viewModel.HasActiveSession;
        _focusItem.IsEnabled = _viewModel.StartFocusCommand.CanExecute(null);
        _restItem.IsEnabled = _viewModel.CanStartRest;
        _widgetItem.Header = _viewModel.WidgetVisibilityActionText;
        _startupItem.IsChecked = _viewModel.StartWithWindows;
        _alwaysOnTopItem.IsChecked = _viewModel.WidgetAlwaysOnTop;
        _voiceAnnouncementsItem.IsChecked = _viewModel.VoiceAnnouncementsEnabled;
        foreach (var pair in _widgetStyleItems)
        {
            pair.Value.IsChecked = pair.Key == _viewModel.SelectedWidgetStyle;
        }

        foreach (var pair in _widgetMotionItems)
        {
            pair.Value.IsChecked = pair.Key == _viewModel.SelectedWidgetMotion;
        }

        foreach (var pair in _widgetOpacityItems)
        {
            pair.Value.IsChecked = pair.Key == _viewModel.SelectedWidgetOpacity;
        }

        foreach (var pair in _colorThemeItems)
        {
            pair.Value.IsChecked = pair.Key == _viewModel.SelectedColorTheme;
        }
    }

    private Icon CreateProgressIcon()
    {
        var accent = ThemeService.GetAccentColor(_viewModel.SelectedColorTheme);
        var drawingAccent = Color.FromArgb(accent.A, accent.R, accent.G, accent.B);
        using var bitmap = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_viewModel.SelectedColorTheme == ColorThemeKind.Brand)
        {
            DrawBrandArc(graphics);
        }
        else
        {
            using var progressPen = new Pen(drawingAccent, 8)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            graphics.DrawArc(progressPen, 7, 7, 50, 50, -78, 300);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DrawBrandArc(Graphics graphics)
    {
        const float startAngle = -78;
        const float totalSweep = 300;
        const int steps = 120;
        var colors = ThemeService.GetBrandColors();
        for (var step = 0; step < steps; step++)
        {
            var fraction = (double)step / (steps - 1);
            var scaled = fraction * (colors.Count - 1);
            var colorIndex = Math.Min((int)Math.Floor(scaled), colors.Count - 2);
            var local = scaled - colorIndex;
            var color = Interpolate(colors[colorIndex], colors[colorIndex + 1], local);
            using var pen = new Pen(Color.FromArgb(color.A, color.R, color.G, color.B), 8)
            {
                StartCap = LineCap.Flat,
                EndCap = LineCap.Flat
            };
            var stepSweep = totalSweep / steps;
            graphics.DrawArc(pen, 7, 7, 50, 50, startAngle + step * stepSweep, stepSweep + 0.8f);
        }

        DrawArcCap(graphics, colors[0], startAngle);
        DrawArcCap(graphics, colors[^1], startAngle + totalSweep);
    }

    private static void DrawArcCap(Graphics graphics, System.Windows.Media.Color color, double angle)
    {
        const double center = 32;
        const double radius = 25;
        const double capRadius = 4;
        var radians = angle * Math.PI / 180;
        var x = center + radius * Math.Cos(radians) - capRadius;
        var y = center + radius * Math.Sin(radians) - capRadius;
        using var brush = new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        graphics.FillEllipse(brush, (float)x, (float)y, (float)(capRadius * 2), (float)(capRadius * 2));
    }

    private static System.Windows.Media.Color Interpolate(
        System.Windows.Media.Color from,
        System.Windows.Media.Color to,
        double fraction) =>
        System.Windows.Media.Color.FromArgb(
            255,
            (byte)Math.Round(from.R + (to.R - from.R) * fraction),
            (byte)Math.Round(from.G + (to.G - from.G) * fraction),
            (byte)Math.Round(from.B + (to.B - from.B) * fraction));

    private static string OpacityLabel(WidgetOpacityKind opacity) => opacity switch
    {
        WidgetOpacityKind.Opacity100 => "100%",
        WidgetOpacityKind.Opacity90 => "90%",
        WidgetOpacityKind.Opacity80 => "80%",
        WidgetOpacityKind.Opacity70 => "70%",
        WidgetOpacityKind.Opacity60 => "60%",
        _ => "90%"
    };

    private static MenuItem CreateMenuItem(string header, Action? action, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        if (action is not null)
        {
            item.Click += (_, _) => action();
        }

        return item;
    }

    private static MenuItem CreateCheckMenuItem(string header, Action action)
    {
        var item = CreateMenuItem(header, action);
        item.IsCheckable = true;
        item.Style = System.Windows.Application.Current.TryFindResource("CompactSubmenuItemStyle") as Style;
        return item;
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);
}
