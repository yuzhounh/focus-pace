using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FocusPace.Controls;

public partial class NumericUpDown : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(NumericUpDown),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(int),
        typeof(NumericUpDown),
        new PropertyMetadata(0, OnLimitChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(int),
        typeof(NumericUpDown),
        new PropertyMetadata(100, OnLimitChanged));

    public NumericUpDown()
    {
        InitializeComponent();
        System.Windows.DataObject.AddPastingHandler(ValueEditor, ValueEditor_OnPaste);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public bool IsEditing => ValueEditor.IsKeyboardFocusWithin;

    public void BeginEdit()
    {
        ValueEditor.Focus();
        ValueEditor.SelectAll();
    }

    private static object CoerceValue(DependencyObject element, object value)
    {
        var control = (NumericUpDown)element;
        return Math.Clamp((int)value, control.Minimum, control.Maximum);
    }

    private static void OnLimitChanged(DependencyObject element, DependencyPropertyChangedEventArgs e) =>
        element.CoerceValue(ValueProperty);

    private void CommitText()
    {
        if (int.TryParse(ValueEditor.Text, out var value))
        {
            Value = Math.Clamp(value, Minimum, Maximum);
        }

        ValueEditor.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateTarget();
    }

    private void ValueEditor_OnPreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(character => !char.IsDigit(character));

    private void ValueEditor_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitText();
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void ValueEditor_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ValueEditor.SelectAll();
    private void ValueEditor_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitText();

    private void Root_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ValueEditor.IsKeyboardFocusWithin)
        {
            return;
        }

        BeginEdit();
        e.Handled = true;
    }

    private void ValueEditor_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(System.Windows.DataFormats.Text)
            || e.DataObject.GetData(System.Windows.DataFormats.Text) is not string text
            || text.Any(character => !char.IsDigit(character)))
        {
            e.CancelCommand();
        }
    }
}
