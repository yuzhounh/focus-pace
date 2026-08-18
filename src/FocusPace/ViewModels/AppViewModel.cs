using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FocusPace.Core;
using FocusPace.Infrastructure;
using FocusPace.Models;
using FocusPace.Services;

namespace FocusPace.ViewModels;

using MediaBrush = System.Windows.Media.Brush;

public sealed class AppViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SessionEngine _engine;
    private readonly AppSettings _settings;
    private readonly SettingsStore _store;
    private readonly StartupService _startupService;
    private readonly IUserActivityMonitor _userActivityMonitor;
    private readonly DispatcherTimer _uiTimer;
    private readonly RelayCommand _pauseResumeCommand;
    private readonly RelayCommand _restartCommand;
    private readonly RelayCommand _startRestCommand;
    private int _lastDisplayedSecond = -1;
    private uint _readyInputBaseline;

    public AppViewModel(
        SessionEngine engine,
        AppSettings settings,
        SettingsStore store,
        StartupService startupService,
        IUserActivityMonitor? userActivityMonitor = null)
    {
        _engine = engine;
        _settings = settings;
        _store = store;
        _startupService = startupService;
        _userActivityMonitor = userActivityMonitor ?? new UserActivityMonitor();
        _settings.StartWithWindows = startupService.IsEnabled();
        _readyInputBaseline = _userActivityMonitor.LastInputTick;

        PrimaryCommand = new RelayCommand(_ => RunPrimaryAction());
        StartFocusCommand = new RelayCommand(_ => StartFocus(), _ => Phase != SessionPhase.Focus);
        _startRestCommand = new RelayCommand(_ => StartRest(), _ => Phase == SessionPhase.Focus);
        StartRestCommand = _startRestCommand;
        _pauseResumeCommand = new RelayCommand(_ => PauseOrResume(), _ => Phase != SessionPhase.Ready);
        PauseResumeCommand = _pauseResumeCommand;
        _restartCommand = new RelayCommand(_ => Restart(), _ => Phase != SessionPhase.Ready);
        RestartCommand = _restartCommand;
        ShowSettingsCommand = new RelayCommand(_ => ShowSettingsRequested?.Invoke(this, EventArgs.Empty));
        SelectWidgetStyleCommand = new RelayCommand(parameter =>
        {
            if (parameter is WidgetStyleKind style)
            {
                SelectedWidgetStyle = style;
            }
        });
        SelectColorThemeCommand = new RelayCommand(parameter =>
        {
            if (parameter is ColorThemeKind colorTheme)
            {
                SelectedColorTheme = colorTheme;
            }
        });
        SelectWidgetMotionCommand = new RelayCommand(parameter =>
        {
            if (parameter is WidgetMotionKind motion)
            {
                SelectedWidgetMotion = motion;
            }
        });
        SelectWidgetOpacityCommand = new RelayCommand(parameter =>
        {
            if (parameter is WidgetOpacityKind opacity)
            {
                SelectedWidgetOpacity = opacity;
            }
        });
        ShowWidgetCommand = new RelayCommand(_ => ShowWidgetRequested?.Invoke(this, EventArgs.Empty));
        HideWidgetCommand = new RelayCommand(_ => HideWidgetRequested?.Invoke(this, EventArgs.Empty));
        ToggleWidgetCommand = new RelayCommand(_ => ToggleWidgetRequested?.Invoke(this, EventArgs.Empty));
        ExitCommand = new RelayCommand(_ => ExitRequested?.Invoke(this, EventArgs.Empty));
        IncreaseFocusCommand = new RelayCommand(_ => FocusMinutes++);
        DecreaseFocusCommand = new RelayCommand(_ => FocusMinutes--, _ => FocusMinutes > 1);
        IncreaseRestCommand = new RelayCommand(_ => RestMinutes++);
        DecreaseRestCommand = new RelayCommand(_ => RestMinutes--, _ => RestMinutes > 1);

        _engine.StateChanged += EngineOnStateChanged;
        _engine.GoalReached += EngineOnGoalReached;
        _uiTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _uiTimer.Tick += UiTimerOnTick;
        _uiTimer.Start();
        RefreshAll();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ShowSettingsRequested;
    public event EventHandler? ShowWidgetRequested;
    public event EventHandler? HideWidgetRequested;
    public event EventHandler? ToggleWidgetRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler<GoalReachedEventArgs>? GoalReached;
    public event EventHandler? AppearanceChanged;

    public ICommand PrimaryCommand { get; }
    public ICommand StartFocusCommand { get; }
    public ICommand StartRestCommand { get; }
    public ICommand PauseResumeCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand SelectWidgetStyleCommand { get; }
    public ICommand SelectColorThemeCommand { get; }
    public ICommand SelectWidgetMotionCommand { get; }
    public ICommand SelectWidgetOpacityCommand { get; }
    public ICommand ShowWidgetCommand { get; }
    public ICommand HideWidgetCommand { get; }
    public ICommand ToggleWidgetCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand IncreaseFocusCommand { get; }
    public ICommand DecreaseFocusCommand { get; }
    public ICommand IncreaseRestCommand { get; }
    public ICommand DecreaseRestCommand { get; }

    public AppSettings Settings => _settings;
    public SessionPhase Phase => _engine.Phase;
    public bool IsPaused => _engine.IsPaused;
    public bool IsReady => Phase == SessionPhase.Ready;
    public bool IsGoalReached => _engine.IsGoalReached;
    public bool CanStartRest => Phase == SessionPhase.Focus;
    public bool CanStartFocus => Phase == SessionPhase.Rest;
    public bool HasActiveSession => Phase != SessionPhase.Ready;
    public bool IsWidgetVisible { get; private set; } = true;
    public IReadOnlyList<WidgetStyleKind> WidgetStyles { get; } = Enum.GetValues<WidgetStyleKind>();
    public IReadOnlyList<ColorThemeKind> ColorThemes { get; } = Enum.GetValues<ColorThemeKind>();
    public string GoalMark => IsGoalReached ? "✓" : string.Empty;
    public string PhaseLabel => IsReady ? "Ready" : IsPaused ? "Paused" : Phase == SessionPhase.Focus ? "Focus Progress" : "Rest Progress";
    public string WidgetPhaseLabel => IsPaused ? "Paused" : Phase == SessionPhase.Rest ? "Rest" : "Focus";
    public string SessionTypeLabel => Phase == SessionPhase.Rest ? "Rest" : "Focus";
    public string TimeText => FormatTime(_engine.Elapsed);
    public string TargetText => FormatTime(_engine.Target);
    public string WidgetText => IsReady ? "Ready · Focus" : $"{WidgetPhaseLabel}   {TimeText}";
    public string WidgetTooltip => IsReady
        ? "Ready when you are"
        : $"{(Phase == SessionPhase.Focus ? "Focused" : "Rested")} {TimeText} of {TargetText}";
    public string DetailText => IsReady
        ? "Set a comfortable pace and begin when you're ready."
        : IsPaused
            ? $"{SessionTypeLabel} paused at {TimeText}"
            : IsGoalReached
                ? $"{SessionTypeLabel} goal reached · continue naturally or move on when ready."
                : $"{SessionTypeLabel} progress · {TimeText} of {TargetText}";
    public string PrimaryActionText => IsReady
        ? "Start Focus"
        : IsPaused
            ? "Resume"
            : Phase == SessionPhase.Focus
                ? IsGoalReached ? "Start Rest" : "Pause"
                : IsGoalReached ? "Start Focus" : "Pause";
    public string PauseResumeText => IsPaused ? "Resume" : "Pause";
    public string WidgetVisibilityActionText => IsWidgetVisible ? "Hide widget" : "Show widget";
    public bool IsBarWidget => SelectedWidgetStyle == WidgetStyleKind.Bar;
    public bool IsRingWidget => SelectedWidgetStyle == WidgetStyleKind.Ring;
    public bool IsFluidWidget => SelectedWidgetStyle == WidgetStyleKind.Fluid;
    public bool IsBrandTheme => SelectedColorTheme == ColorThemeKind.Brand;
    public bool IsOceanTheme => SelectedColorTheme == ColorThemeKind.Ocean;
    public bool IsVioletTheme => SelectedColorTheme == ColorThemeKind.Violet;
    public bool IsMintTheme => SelectedColorTheme == ColorThemeKind.Mint;
    public bool IsAmberTheme => SelectedColorTheme == ColorThemeKind.Amber;
    public bool IsRoseTheme => SelectedColorTheme == ColorThemeKind.Rose;
    public bool IsSilverTheme => SelectedColorTheme == ColorThemeKind.Silver;
    public bool IsStaticWidget => SelectedWidgetMotion == WidgetMotionKind.Static;
    public bool IsWidgetAnimated => SelectedWidgetMotion == WidgetMotionKind.Dynamic;
    public bool IsWidgetOpacity100 => SelectedWidgetOpacity == WidgetOpacityKind.Opacity100;
    public bool IsWidgetOpacity90 => SelectedWidgetOpacity == WidgetOpacityKind.Opacity90;
    public bool IsWidgetOpacity80 => SelectedWidgetOpacity == WidgetOpacityKind.Opacity80;
    public bool IsWidgetOpacity70 => SelectedWidgetOpacity == WidgetOpacityKind.Opacity70;
    public bool IsWidgetOpacity60 => SelectedWidgetOpacity == WidgetOpacityKind.Opacity60;
    public double WidgetOpacity => SelectedWidgetOpacity switch
    {
        WidgetOpacityKind.Opacity100 => 1,
        WidgetOpacityKind.Opacity90 => 0.90,
        WidgetOpacityKind.Opacity80 => 0.80,
        WidgetOpacityKind.Opacity70 => 0.70,
        WidgetOpacityKind.Opacity60 => 0.60,
        WidgetOpacityKind.Opacity40 => 0.40,
        WidgetOpacityKind.Opacity20 => 0.20,
        WidgetOpacityKind.Opacity0 => 0,
        WidgetOpacityKind.Opacity95 => 0.90,
        _ => 1
    };
    public double WidgetWidth => IsBarWidget ? 176 : 116;
    public double WidgetHeight => IsBarWidget ? 46 : 116;
    public double Progress => IsReady || _engine.Target <= TimeSpan.Zero
        ? 0
        : Math.Clamp(_engine.Elapsed.TotalMilliseconds / _engine.Target.TotalMilliseconds, 0, 1);
    public MediaBrush ProgressFillBrush => IsPaused
        ? ResourceBrush("ProgressPausedBrush")
        : IsGoalReached
            ? ResourceBrush("ProgressCompleteBrush")
            : Phase == SessionPhase.Rest
                ? ResourceBrush("ProgressRestBrush")
                : Progress switch
                {
                    < 0.3 => ResourceBrush("ProgressEarlyBrush"),
                    < 0.7 => ResourceBrush("ProgressMidBrush"),
                    _ => ResourceBrush("ProgressLateBrush")
                };
    public MediaBrush FluidPhaseTextBrush => AdaptiveFluidTextBrush(0.58);
    public MediaBrush FluidTimeTextBrush => AdaptiveFluidTextBrush(0.40);
    public MediaBrush ProgressTextBrush => AdaptiveTextBrush(ProgressFillBrush);

    public WidgetStyleKind SelectedWidgetStyle
    {
        get => _settings.WidgetStyle;
        set
        {
            if (_settings.WidgetStyle == value)
            {
                return;
            }

            _settings.WidgetStyle = value;
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBarWidget));
            OnPropertyChanged(nameof(IsRingWidget));
            OnPropertyChanged(nameof(IsFluidWidget));
            OnPropertyChanged(nameof(WidgetWidth));
            OnPropertyChanged(nameof(WidgetHeight));
            AppearanceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ColorThemeKind SelectedColorTheme
    {
        get => _settings.ColorTheme;
        set
        {
            if (_settings.ColorTheme == value)
            {
                return;
            }

            _settings.ColorTheme = value;
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBrandTheme));
            OnPropertyChanged(nameof(IsOceanTheme));
            OnPropertyChanged(nameof(IsVioletTheme));
            OnPropertyChanged(nameof(IsMintTheme));
            OnPropertyChanged(nameof(IsAmberTheme));
            OnPropertyChanged(nameof(IsRoseTheme));
            OnPropertyChanged(nameof(IsSilverTheme));
            AppearanceChanged?.Invoke(this, EventArgs.Empty);
            RefreshAll();
        }
    }

    public WidgetMotionKind SelectedWidgetMotion
    {
        get => _settings.WidgetMotion;
        set
        {
            if (_settings.WidgetMotion == value)
            {
                return;
            }

            _settings.WidgetMotion = value;
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStaticWidget));
            OnPropertyChanged(nameof(IsWidgetAnimated));
        }
    }

    public WidgetOpacityKind SelectedWidgetOpacity
    {
        get => _settings.WidgetOpacity;
        set
        {
            if (_settings.WidgetOpacity == value)
            {
                return;
            }

            _settings.WidgetOpacity = value;
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWidgetOpacity100));
            OnPropertyChanged(nameof(IsWidgetOpacity90));
            OnPropertyChanged(nameof(IsWidgetOpacity80));
            OnPropertyChanged(nameof(IsWidgetOpacity70));
            OnPropertyChanged(nameof(IsWidgetOpacity60));
            OnPropertyChanged(nameof(WidgetOpacity));
        }
    }

    public int FocusMinutes
    {
        get => _settings.FocusMinutes;
        set
        {
            var clamped = Math.Clamp(value, 1, 240);
            if (_settings.FocusMinutes == clamped)
            {
                return;
            }

            _settings.FocusMinutes = clamped;
            OnPropertyChanged();
            if (Phase == SessionPhase.Focus)
            {
                _engine.UpdateTarget(TimeSpan.FromMinutes(clamped));
            }
            else
            {
                Save();
            }

            CommandStatesChanged();
        }
    }

    public int RestMinutes
    {
        get => _settings.RestMinutes;
        set
        {
            var clamped = Math.Clamp(value, 1, 60);
            if (_settings.RestMinutes == clamped)
            {
                return;
            }

            _settings.RestMinutes = clamped;
            OnPropertyChanged();
            if (Phase == SessionPhase.Rest)
            {
                _engine.UpdateTarget(TimeSpan.FromMinutes(clamped));
            }
            else
            {
                Save();
            }

            CommandStatesChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows == value)
            {
                return;
            }

            try
            {
                _startupService.SetEnabled(value);
                _settings.StartWithWindows = value;
                Save();
                OnPropertyChanged();
            }
            catch
            {
                _settings.StartWithWindows = _startupService.IsEnabled();
                OnPropertyChanged();
            }
        }
    }

    public bool WidgetAlwaysOnTop
    {
        get => _settings.WidgetAlwaysOnTop;
        set
        {
            if (_settings.WidgetAlwaysOnTop == value)
            {
                return;
            }

            _settings.WidgetAlwaysOnTop = value;
            Save();
            OnPropertyChanged();
        }
    }

    public void StartFocus() => _engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(FocusMinutes));
    public void StartFocusExtension() => _engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(3));
    public void StartRest() => _engine.Start(SessionPhase.Rest, TimeSpan.FromMinutes(RestMinutes));
    public void Restart() => _engine.Restart();

    public void PauseOrResume()
    {
        if (_engine.IsPaused)
        {
            _engine.Resume();
        }
        else
        {
            _engine.Pause();
        }
    }

    public void PauseForSystemEvent()
    {
        if (Phase != SessionPhase.Ready && !IsPaused)
        {
            _engine.Pause();
        }
    }

    public void RunWidgetAction(SessionPhase phaseAtClickStart)
    {
        if (phaseAtClickStart == SessionPhase.Ready)
        {
            if (IsReady)
            {
                StartFocus();
            }

            return;
        }

        if (IsReady)
        {
            StartFocus();
        }
        else if (IsPaused)
        {
            _engine.Resume();
        }
        else if (Phase == SessionPhase.Focus && IsGoalReached)
        {
            StartRest();
        }
        else if (Phase == SessionPhase.Rest && IsGoalReached)
        {
            StartFocus();
        }
        else
        {
            ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RunWidgetDoubleClickAction(SessionPhase phaseAtClickStart)
    {
        if (phaseAtClickStart == SessionPhase.Focus)
        {
            StartRest();
        }
        else
        {
            StartFocus();
        }
    }

    public void SaveWidgetPlacement(string deviceName, double relativeX, double relativeY)
    {
        _settings.WidgetPlacement.HasValue = true;
        _settings.WidgetPlacement.MonitorDeviceName = deviceName;
        _settings.WidgetPlacement.RelativeX = Math.Clamp(relativeX, 0, 1);
        _settings.WidgetPlacement.RelativeY = Math.Clamp(relativeY, 0, 1);
        Save();
    }

    public void SetWidgetVisibility(bool visible)
    {
        if (IsWidgetVisible == visible)
        {
            return;
        }

        IsWidgetVisible = visible;
        OnPropertyChanged(nameof(IsWidgetVisible));
        OnPropertyChanged(nameof(WidgetVisibilityActionText));
    }

    public void ResetSessionForExplicitExit() => _engine.ResetToReady();

    public void Save()
    {
        _settings.Session = _engine.Phase == SessionPhase.Ready ? null : _engine.CreateSnapshot();
        _store.Save(_settings);
    }

    public void Dispose()
    {
        _uiTimer.Stop();
        _uiTimer.Tick -= UiTimerOnTick;
        _engine.StateChanged -= EngineOnStateChanged;
        _engine.GoalReached -= EngineOnGoalReached;
    }

    private void RunPrimaryAction()
    {
        if (IsReady)
        {
            StartFocus();
        }
        else if (IsPaused)
        {
            _engine.Resume();
        }
        else if (Phase == SessionPhase.Focus && IsGoalReached)
        {
            StartRest();
        }
        else if (Phase == SessionPhase.Rest && IsGoalReached)
        {
            StartFocus();
        }
        else
        {
            _engine.Pause();
        }
    }

    private void UiTimerOnTick(object? sender, EventArgs e)
    {
        ApplyAutomaticStateTransitions();
        _engine.Pulse();
        var second = (int)_engine.Elapsed.TotalSeconds;
        if (second != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = second;
            RefreshAll();
        }
    }

    private void EngineOnStateChanged(object? sender, EventArgs e)
    {
        if (Phase == SessionPhase.Ready)
        {
            _readyInputBaseline = _userActivityMonitor.LastInputTick;
        }

        Save();
        RefreshAll();
    }

    private void EngineOnGoalReached(object? sender, GoalReachedEventArgs e)
    {
        if (e.Phase == SessionPhase.Rest)
        {
            _engine.ResetToReady();
        }

        GoalReached?.Invoke(this, e);
    }

    private void ApplyAutomaticStateTransitions()
    {
        if (Phase == SessionPhase.Ready)
        {
            var latestInput = _userActivityMonitor.LastInputTick;
            if (latestInput != 0 && latestInput != _readyInputBaseline)
            {
                StartFocus();
            }

            return;
        }

        if (Phase == SessionPhase.Focus &&
            _userActivityMonitor.IdleTime >= TimeSpan.FromMinutes(RestMinutes))
        {
            _engine.ResetToReady();
        }
    }

    private void RefreshAll()
    {
        OnPropertyChanged(nameof(Phase));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(IsGoalReached));
        OnPropertyChanged(nameof(CanStartRest));
        OnPropertyChanged(nameof(CanStartFocus));
        OnPropertyChanged(nameof(HasActiveSession));
        OnPropertyChanged(nameof(GoalMark));
        OnPropertyChanged(nameof(PhaseLabel));
        OnPropertyChanged(nameof(WidgetPhaseLabel));
        OnPropertyChanged(nameof(SessionTypeLabel));
        OnPropertyChanged(nameof(TimeText));
        OnPropertyChanged(nameof(TargetText));
        OnPropertyChanged(nameof(WidgetText));
        OnPropertyChanged(nameof(WidgetTooltip));
        OnPropertyChanged(nameof(DetailText));
        OnPropertyChanged(nameof(PrimaryActionText));
        OnPropertyChanged(nameof(PauseResumeText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressFillBrush));
        OnPropertyChanged(nameof(ProgressTextBrush));
        OnPropertyChanged(nameof(FluidPhaseTextBrush));
        OnPropertyChanged(nameof(FluidTimeTextBrush));
        CommandStatesChanged();
    }

    private void CommandStatesChanged()
    {
        _pauseResumeCommand.RaiseCanExecuteChanged();
        _restartCommand.RaiseCanExecuteChanged();
        _startRestCommand.RaiseCanExecuteChanged();
        (StartFocusCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DecreaseFocusCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DecreaseRestCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private static string FormatTime(TimeSpan time)
    {
        var totalHours = (int)time.TotalHours;
        return totalHours > 0
            ? $"{totalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private static MediaBrush ResourceBrush(string key) =>
        System.Windows.Application.Current.Resources[key] as MediaBrush ?? System.Windows.Media.Brushes.SlateGray;

    private MediaBrush AdaptiveFluidTextBrush(double coverageThreshold)
    {
        if (Progress < coverageThreshold)
        {
            return ResourceBrush("WidgetTextBrush");
        }

        return AdaptiveTextBrush(ProgressFillBrush);
    }

    private static MediaBrush AdaptiveTextBrush(MediaBrush backgroundBrush)
    {
        var darkText = ResourceBrush("TextOnLightBackgroundBrush");
        var lightText = ResourceBrush("TextOnDarkBackgroundBrush");
        var perceivedBrightness = backgroundBrush switch
        {
            SolidColorBrush background => PerceivedBrightness(background.Color),
            GradientBrush gradient when gradient.GradientStops.Count > 0 =>
                gradient.GradientStops.Average(stop => PerceivedBrightness(stop.Color)),
            _ => 1d
        };
        return perceivedBrightness < 0.60 ? lightText : darkText;
    }

    private static double PerceivedBrightness(System.Windows.Media.Color color) =>
        (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255d;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class NotifyPropertyChangedInvocatorAttribute : Attribute
{
}
