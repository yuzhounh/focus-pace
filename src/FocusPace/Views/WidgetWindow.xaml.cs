using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using FocusPace.Models;
using FocusPace.ViewModels;
using Forms = System.Windows.Forms;

namespace FocusPace.Views;

public partial class WidgetWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WmMouseActivate = 0x0021;
    private const int WmRightButtonDown = 0x0204;
    private const int MouseActivateNoActivate = 3;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private bool _dragging;
    private bool _moved;
    private bool _doubleClick;
    private SessionPhase _phaseAtClickStart;
    private readonly System.Windows.Threading.DispatcherTimer _singleClickTimer;
    private NativePoint _dragStart;
    private NativeRect _windowStart;
    private IntPtr _handle;
    private HwndSource? _source;

    public WidgetWindow()
    {
        InitializeComponent();
        _singleClickTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Forms.SystemInformation.DoubleClickTime)
        };
        _singleClickTimer.Tick += (_, _) =>
        {
            _singleClickTimer.Stop();
            ViewModel.RunWidgetAction(_phaseAtClickStart);
        };
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
    }

    public NativeRect PhysicalBounds
    {
        get
        {
            GetWindowRect(_handle, out var rect);
            return rect;
        }
    }

    private AppViewModel ViewModel => (AppViewModel)DataContext;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(_handle, GwlExstyle).ToInt64();
        SetWindowLongPtr(_handle, GwlExstyle, new IntPtr(style | WsExToolWindow));
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WindowProcedure);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ToolTip = ViewModel.WidgetTooltip;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppViewModel.WidgetTooltip))
            {
                ToolTip = ViewModel.WidgetTooltip;
            }
            else if (args.PropertyName == nameof(AppViewModel.SelectedWidgetStyle))
            {
                ApplyWidgetDimensions();
            }
        };
        ApplyWidgetDimensions();
    }

    private void ApplyWidgetDimensions()
    {
        var width = ViewModel.WidgetWidth;
        var height = ViewModel.WidgetHeight;

        MinWidth = 0;
        MinHeight = 0;
        MaxWidth = double.PositiveInfinity;
        MaxHeight = double.PositiveInfinity;
        Width = width;
        Height = height;
        MinWidth = width;
        MaxWidth = width;
        MinHeight = height;
        MaxHeight = height;

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            UpdateLayout();
            var dpi = Math.Max(96u, GetDpiForWindow(_handle));
            var physicalWidth = (int)Math.Round(width * dpi / 96d);
            var physicalHeight = (int)Math.Round(height * dpi / 96d);
            SetWindowPos(
                _handle,
                IntPtr.Zero,
                0,
                0,
                physicalWidth,
                physicalHeight,
                SwpNoActivate | SwpNoMove | SwpNoZOrder);
            RestorePlacement(ViewModel.Settings.WidgetPlacement);
        });
    }

    private void RestorePlacement(WidgetPlacement placement)
    {
        var screens = Forms.Screen.AllScreens;
        var usesLegacyTopRightPosition = placement.HasValue
                                         && placement.RelativeX > 0.97
                                         && placement.RelativeY < 0.08;
        var useSaferDefault = !placement.HasValue || usesLegacyTopRightPosition;
        var screen = placement.HasValue
            ? screens.FirstOrDefault(item => item.DeviceName == placement.MonitorDeviceName) ?? Forms.Screen.PrimaryScreen
            : Forms.Screen.PrimaryScreen;
        if (screen is null)
        {
            return;
        }

        GetWindowRect(_handle, out var rect);
        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);
        var working = screen.WorkingArea;
        var availableX = Math.Max(0, working.Width - width);
        var availableY = Math.Max(0, working.Height - height);
        var dpi = GetDpiForWindow(_handle);
        var safeEdgePadding = (int)Math.Round(72 * Math.Max(96u, dpi) / 96d);
        var x = useSaferDefault
            ? working.Right - width - safeEdgePadding
            : working.Left + (int)Math.Round(Math.Clamp(placement.RelativeX, 0, 1) * availableX);
        var y = useSaferDefault
            ? working.Top + safeEdgePadding
            : working.Top + (int)Math.Round(Math.Clamp(placement.RelativeY, 0, 1) * availableY);
        SetWindowPos(_handle, IntPtr.Zero, x, y, 0, 0, SwpNoActivate | SwpNoSize);
        if (usesLegacyTopRightPosition)
        {
            Dispatcher.BeginInvoke(SavePlacement);
        }
    }

    private void Pill_OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var reveal = new DoubleAnimation
        {
            To = 1,
            Duration = TimeSpan.Zero,
            FillBehavior = FillBehavior.HoldEnd
        };
        Pill.BeginAnimation(UIElement.OpacityProperty, reveal);
    }

    private void Pill_OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) =>
        Pill.BeginAnimation(UIElement.OpacityProperty, null);

    protected override void OnClosed(EventArgs e)
    {
        _singleClickTimer.Stop();
        _source?.RemoveHook(WindowProcedure);
        base.OnClosed(e);
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmMouseActivate)
        {
            var mouseMessage = (int)((lParam.ToInt64() >> 16) & 0xFFFF);
            if (mouseMessage != WmRightButtonDown)
            {
                handled = true;
                return new IntPtr(MouseActivateNoActivate);
            }
        }

        return IntPtr.Zero;
    }

    private void Pill_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            _singleClickTimer.Stop();
            _doubleClick = true;
        }
        else
        {
            _doubleClick = false;
            _phaseAtClickStart = ViewModel.Phase;
        }

        if (!GetCursorPos(out _dragStart) || !GetWindowRect(_handle, out _windowStart))
        {
            return;
        }

        _dragging = true;
        _moved = false;
        Pill.CaptureMouse();
        e.Handled = true;
    }

    private void Pill_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging || !GetCursorPos(out var current))
        {
            return;
        }

        var deltaX = current.X - _dragStart.X;
        var deltaY = current.Y - _dragStart.Y;
        if (!_moved && Math.Abs(deltaX) + Math.Abs(deltaY) < 5)
        {
            return;
        }

        _moved = true;
        SetWindowPos(_handle, IntPtr.Zero, _windowStart.Left + deltaX, _windowStart.Top + deltaY, 0, 0, SwpNoActivate | SwpNoSize);
    }

    private void Pill_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Pill.ReleaseMouseCapture();
        if (_moved)
        {
            SavePlacement();
        }
        else if (_doubleClick)
        {
            _doubleClick = false;
            _singleClickTimer.Stop();
            ViewModel.RunWidgetDoubleClickAction(_phaseAtClickStart);
        }
        else
        {
            _singleClickTimer.Stop();
            _singleClickTimer.Start();
        }

        e.Handled = true;
    }

    private void SavePlacement()
    {
        if (!GetWindowRect(_handle, out var rect))
        {
            return;
        }

        var center = new System.Drawing.Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        var screen = Forms.Screen.FromPoint(center);
        var working = screen.WorkingArea;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var availableX = Math.Max(1, working.Width - width);
        var availableY = Math.Max(1, working.Height - height);
        var relativeX = (double)(rect.Left - working.Left) / availableX;
        var relativeY = (double)(rect.Top - working.Top) / availableY;
        ViewModel.SaveWidgetPlacement(screen.DeviceName, relativeX, relativeY);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern IntPtr GetWindowLong32(IntPtr hwnd, int index);

    private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index) => IntPtr.Size == 8
        ? GetWindowLongPtr64(hwnd, index)
        : GetWindowLong32(hwnd, index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern IntPtr SetWindowLong32(IntPtr hwnd, int index, IntPtr value);

    private static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value) => IntPtr.Size == 8
        ? SetWindowLongPtr64(hwnd, index, value)
        : SetWindowLong32(hwnd, index, value);
}
