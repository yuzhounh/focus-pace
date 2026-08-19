using System.Windows;
using System.Windows.Threading;
using FocusPace.Models;
using FocusPace.ViewModels;
using Forms = System.Windows.Forms;

namespace FocusPace.Views;

public partial class GoalToastWindow : Window
{
    private enum ToastKind
    {
        None,
        FocusEndingSoon,
        RestComplete
    }

    private readonly AppViewModel _viewModel;
    private readonly DispatcherTimer _dismissTimer;
    private ToastKind _toastKind;

    public GoalToastWindow(AppViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        _dismissTimer = new DispatcherTimer();
        _dismissTimer.Tick += (_, _) =>
        {
            HideToast();
        };
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    public void ShowRestComplete(WidgetWindow widget)
    {
        _toastKind = ToastKind.RestComplete;
        TitleText.Text = "Rest complete";
        MessageText.Text = "Ready when you are.";
        ShowToast(widget, null);
    }

    public void ShowFocusEndingSoon(WidgetWindow widget)
    {
        _toastKind = ToastKind.FocusEndingSoon;
        TitleText.Text = "Focus ending soon";
        MessageText.Text = "3 minutes remaining.";
        ShowToast(widget, TimeSpan.FromSeconds(15));
    }

    private void ShowToast(WidgetWindow widget, TimeSpan? duration)
    {
        _dismissTimer.Stop();
        var widgetBounds = widget.PhysicalBounds;
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(widgetBounds.Left, widgetBounds.Top));
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        var widthPixels = (int)Math.Round(Width * dpi.DpiScaleX);
        var widgetWidth = widgetBounds.Right - widgetBounds.Left;
        var centeredLeft = widgetBounds.Left + ((widgetWidth - widthPixels) / 2);
        var minimumLeft = screen.WorkingArea.Left + 12;
        var maximumLeft = screen.WorkingArea.Right - widthPixels - 12;
        Left = Math.Clamp(centeredLeft, minimumLeft, Math.Max(minimumLeft, maximumLeft)) / dpi.DpiScaleX;
        Top = (widgetBounds.Bottom + 10) / dpi.DpiScaleY;

        Opacity = 1;
        Show();
        if (duration is not null)
        {
            _dismissTimer.Interval = duration.Value;
            _dismissTimer.Start();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        base.OnClosed(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppViewModel.Phase))
        {
            return;
        }

        if ((_toastKind == ToastKind.RestComplete && _viewModel.Phase == SessionPhase.Focus) ||
            (_toastKind == ToastKind.FocusEndingSoon && _viewModel.Phase != SessionPhase.Focus))
        {
            HideToast();
        }
    }

    private void HideToast()
    {
        _dismissTimer.Stop();
        _toastKind = ToastKind.None;
        Hide();
    }
}
