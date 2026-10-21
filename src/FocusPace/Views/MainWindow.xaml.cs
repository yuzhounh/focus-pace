using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace FocusPace.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var version = typeof(MainWindow).Assembly.GetName().Version;
        VersionText.Text = version is null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";
        SourceInitialized += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var roundedCorners = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref roundedCorners, sizeof(int));
            }
        };
        Loaded += (_, _) =>
        {
            var workArea = SystemParameters.WorkArea;
            if (Height > workArea.Height - 16)
            {
                Height = Math.Max(MinHeight, workArea.Height - 16);
            }
        };
    }

    public bool AllowClose { get; set; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public void OpenSettingsPage() => SetSettingsOpen(true);

    private void SetSettingsOpen(bool open)
    {
        SettingsView.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        MainView.Visibility = open ? Visibility.Collapsed : Visibility.Visible;
        SettingsButton.Content = open ? "\uE72B" : "\uE713";
        SettingsButton.ToolTip = open ? "Back" : "Settings";
    }

    private void Settings_OnClick(object sender, RoutedEventArgs e) =>
        SetSettingsOpen(SettingsView.Visibility != Visibility.Visible);

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape && SettingsView.Visibility == Visibility.Visible)
        {
            SetSettingsOpen(false);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void Minimize_OnClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

    private void FocusGoalCard_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FocusGoalInput.IsEditing)
        {
            return;
        }

        FocusGoalInput.BeginEdit();
        e.Handled = true;
    }

    private void RestGoalCard_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RestGoalInput.IsEditing)
        {
            return;
        }

        RestGoalInput.BeginEdit();
        e.Handled = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}
