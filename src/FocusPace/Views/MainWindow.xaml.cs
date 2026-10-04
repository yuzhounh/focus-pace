using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace FocusPace.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
