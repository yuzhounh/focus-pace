using System.Windows;
using System.Windows.Threading;
using FocusPace.Models;
using FocusPace.ViewModels;
using Forms = System.Windows.Forms;

namespace FocusPace.Views;

public partial class GoalToastWindow : Window
{
    private readonly AppViewModel _viewModel;
    private readonly DispatcherTimer _dismissTimer;
    private SessionPhase _phase;

    public GoalToastWindow(AppViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(9) };
        _dismissTimer.Tick += (_, _) =>
        {
            _dismissTimer.Stop();
            Hide();
        };
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    public void ShowGoal(SessionPhase phase, TimeSpan target, WidgetWindow widget)
    {
        _phase = phase;
        if (phase == SessionPhase.Focus)
        {
            TitleText.Text = "Focus goal reached";
            MessageText.Text = $"{target.TotalMinutes:0} min focused. Take a rest when you're ready.";
            ActionButton.Content = "Start Rest";
        }
        else
        {
            TitleText.Text = "Rest complete";
            MessageText.Text = "Ready when you are.";
            ActionButton.Content = "Start Focus";
        }

        var widgetBounds = widget.PhysicalBounds;
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(widgetBounds.Left, widgetBounds.Top));
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        var widthPixels = (int)Math.Round(Width * dpi.DpiScaleX);
        var heightPixels = (int)Math.Round(Height * dpi.DpiScaleY);
        Left = (screen.WorkingArea.Right - widthPixels - 26) / dpi.DpiScaleX;
        Top = (widgetBounds.Bottom + 10) / dpi.DpiScaleY;
        if ((Top + Height) * dpi.DpiScaleY > screen.WorkingArea.Bottom)
        {
            Top = (widgetBounds.Top - heightPixels - 10) / dpi.DpiScaleY;
        }

        Opacity = 1;
        Show();
        _dismissTimer.Stop();
        _dismissTimer.Start();
    }

    private void ActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_phase == SessionPhase.Focus)
        {
            _viewModel.StartRest();
        }
        else
        {
            _viewModel.StartFocus();
        }

        _dismissTimer.Stop();
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        base.OnClosed(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.Phase) &&
            _phase == SessionPhase.Rest &&
            _viewModel.Phase == SessionPhase.Focus)
        {
            _dismissTimer.Stop();
            Hide();
        }
    }
}
