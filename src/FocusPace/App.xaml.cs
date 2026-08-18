using System.Threading;
using System.Windows;
using FocusPace.Core;
using FocusPace.Services;
using FocusPace.ViewModels;
using FocusPace.Views;
using Microsoft.Win32;

namespace FocusPace;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\FocusPace.SingleInstance";
    private const string ActivationEventName = @"Local\FocusPace.Activate";
    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _activationWait;
    private AppViewModel? _viewModel;
    private MainWindow? _mainWindow;
    private WidgetWindow? _widgetWindow;
    private GoalToastWindow? _goalToastWindow;
    private TrayIconService? _trayIcon;
    private readonly List<RestOverlayWindow> _restOverlayWindows = [];
    private bool _restoreWidgetAfterRestOverlay;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _singleInstanceMutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        _activationWait = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, _) => Dispatcher.BeginInvoke(ShowSettings),
            null,
            Timeout.Infinite,
            false);

        var store = new SettingsStore();
        var settings = store.Load();
        ThemeService.Apply(Resources, settings.ColorTheme);
        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;

        var engine = new SessionEngine(SystemClock.Instance);
        engine.TryRestore(settings.Session);
        _viewModel = new AppViewModel(engine, settings, store, new StartupService());
        _viewModel.ShowSettingsRequested += (_, _) => ShowSettings();
        _viewModel.ShowWidgetRequested += (_, _) => ShowWidget();
        _viewModel.HideWidgetRequested += (_, _) => HideWidget();
        _viewModel.ToggleWidgetRequested += (_, _) => ToggleWidget();
        _viewModel.AppearanceChanged += (_, _) => ThemeService.Apply(Resources, _viewModel.SelectedColorTheme);
        _viewModel.ExitRequested += (_, _) => ExitApplication();
        _viewModel.GoalReached += ViewModelOnGoalReached;

        _mainWindow = new MainWindow { DataContext = _viewModel };
        _widgetWindow = new WidgetWindow { DataContext = _viewModel };
        _goalToastWindow = new GoalToastWindow(_viewModel);
        _trayIcon = new TrayIconService(_viewModel);

        SystemEvents.SessionSwitch += SystemEventsOnSessionSwitch;
        SystemEvents.PowerModeChanged += SystemEventsOnPowerModeChanged;
        SessionEnding += OnSessionEnding;

        _widgetWindow.Show();
        _viewModel.SetWidgetVisibility(true);
        if (!e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase))
        {
            _mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        SystemEvents.SessionSwitch -= SystemEventsOnSessionSwitch;
        SystemEvents.PowerModeChanged -= SystemEventsOnPowerModeChanged;
        _activationWait?.Unregister(null);
        _activationEvent?.Dispose();
        if (_singleInstanceMutex is not null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }

            _singleInstanceMutex.Dispose();
        }

        _trayIcon?.Dispose();
        _viewModel?.Dispose();
        base.OnExit(e);
    }

    private void ShowSettings()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private void ShowWidget()
    {
        if (_widgetWindow is null)
        {
            return;
        }

        _widgetWindow.Show();
        _widgetWindow.SetCurrentValue(Window.TopmostProperty, false);
        _widgetWindow.SetCurrentValue(Window.TopmostProperty, _viewModel?.WidgetAlwaysOnTop == true);
        _viewModel?.SetWidgetVisibility(true);
    }

    private void HideWidget()
    {
        _widgetWindow?.Hide();
        _viewModel?.SetWidgetVisibility(false);
    }

    private void ToggleWidget()
    {
        if (_widgetWindow?.IsVisible == true)
        {
            HideWidget();
        }
        else
        {
            ShowWidget();
        }
    }

    private void ViewModelOnGoalReached(object? sender, GoalReachedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.Phase == Models.SessionPhase.Focus)
        {
            _viewModel.StartRest();
            ShowRestOverlay();
            return;
        }

        CloseRestOverlay();
        if (_goalToastWindow is not null && _widgetWindow is not null)
        {
            _goalToastWindow.ShowGoal(e.Phase, e.Target, _widgetWindow);
        }
    }

    private void ShowRestOverlay()
    {
        CloseRestOverlay(false);
        _restoreWidgetAfterRestOverlay = _widgetWindow?.IsVisible == true;
        if (_restoreWidgetAfterRestOverlay)
        {
            HideWidget();
        }

        var screens = System.Windows.Forms.Screen.AllScreens;
        if (screens.Length == 0)
        {
            return;
        }

        var primary = System.Windows.Forms.Screen.PrimaryScreen ?? screens[0];
        var wallpaperPath = WallpaperService.GetCurrentWallpaperPath();
        foreach (var screen in screens.OrderBy(screen => screen.Primary))
        {
            var overlay = new RestOverlayWindow(screen, screen.DeviceName == primary.DeviceName, wallpaperPath, _viewModel!);
            overlay.ExtendFocusRequested += RestOverlayOnExtendFocusRequested;
            overlay.LeaveFullScreenRequested += RestOverlayOnLeaveFullScreenRequested;
            _restOverlayWindows.Add(overlay);
            overlay.Show();
        }
    }

    private void RestOverlayOnExtendFocusRequested(object? sender, EventArgs e)
    {
        CloseRestOverlay();
        _viewModel?.StartFocusExtension();
    }

    private void RestOverlayOnLeaveFullScreenRequested(object? sender, EventArgs e) => CloseRestOverlay();

    private void CloseRestOverlay(bool restoreWidget = true)
    {
        foreach (var overlay in _restOverlayWindows.ToArray())
        {
            overlay.ExtendFocusRequested -= RestOverlayOnExtendFocusRequested;
            overlay.LeaveFullScreenRequested -= RestOverlayOnLeaveFullScreenRequested;
            overlay.CloseOverlay();
        }

        _restOverlayWindows.Clear();
        if (restoreWidget && _restoreWidgetAfterRestOverlay)
        {
            ShowWidget();
        }

        _restoreWidgetAfterRestOverlay = false;
    }

    private void SystemEventsOnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)
        {
            Dispatcher.BeginInvoke(() => _viewModel?.PauseForSystemEvent());
        }
    }

    private void SystemEventsOnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            Dispatcher.BeginInvoke(() => _viewModel?.PauseForSystemEvent());
        }
    }

    private void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
        Dispatcher.BeginInvoke(() => ThemeService.Apply(Resources, _viewModel?.SelectedColorTheme ?? Models.ColorThemeKind.Brand));

    private void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        _viewModel?.PauseForSystemEvent();
        _viewModel?.Save();
    }

    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        CloseRestOverlay(false);
        _viewModel?.ResetSessionForExplicitExit();
        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.Close();
        }

        _goalToastWindow?.Close();
        _widgetWindow?.Close();
        Shutdown();
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var activation = EventWaitHandle.OpenExisting(ActivationEventName);
            activation.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
    }
}
