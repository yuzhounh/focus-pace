using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FocusPace.Controls;

using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

public sealed class AnimatedBarProgressControl : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(AnimatedBarProgressControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register(
        nameof(ProgressBrush), typeof(MediaBrush), typeof(AnimatedBarProgressControl),
        new FrameworkPropertyMetadata(MediaBrushes.CornflowerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register(
        nameof(IsAnimated), typeof(bool), typeof(AnimatedBarProgressControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnIsAnimatedChanged));

    private readonly DispatcherTimer _animationTimer;
    private double _animationPhase;

    public AnimatedBarProgressControl()
    {
        _animationTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(45)
        };
        _animationTimer.Tick += (_, _) =>
        {
            _animationPhase = (_animationPhase + 0.025) % 1;
            InvalidateVisual();
        };
        Loaded += (_, _) => UpdateAnimationState();
        Unloaded += (_, _) => _animationTimer.Stop();
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public MediaBrush ProgressBrush
    {
        get => (MediaBrush)GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }

    public bool IsAnimated
    {
        get => (bool)GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var progressWidth = ActualWidth * Math.Clamp(Progress, 0, 1);
        if (progressWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var radius = Math.Min(12, progressWidth / 2);
        var progressBounds = new Rect(0, 0, progressWidth, ActualHeight);
        drawingContext.DrawRoundedRectangle(ProgressBrush, null, progressBounds, radius, radius);
        if (!IsAnimated || !SystemParameters.ClientAreaAnimation || progressWidth < 12)
        {
            return;
        }

        var bandWidth = Math.Min(Math.Max(18, ActualWidth * 0.22), Math.Max(12, progressWidth * 0.65));
        var bandX = -bandWidth + _animationPhase * (progressWidth + bandWidth * 2);
        var shimmer = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0.5),
            EndPoint = new System.Windows.Point(1, 0.5)
        };
        shimmer.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 255, 255, 255), 0));
        shimmer.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(82, 255, 255, 255), 0.5));
        shimmer.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 255, 255, 255), 1));
        shimmer.Freeze();

        drawingContext.PushClip(new RectangleGeometry(progressBounds, radius, radius));
        drawingContext.DrawRectangle(shimmer, null, new Rect(bandX, 0, bandWidth, ActualHeight));
        drawingContext.Pop();
    }

    private static void OnIsAnimatedChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnimatedBarProgressControl)element;
        control.UpdateAnimationState();
        control.InvalidateVisual();
    }

    private void UpdateAnimationState()
    {
        if (IsLoaded && IsAnimated && SystemParameters.ClientAreaAnimation)
        {
            _animationTimer.Start();
        }
        else
        {
            _animationTimer.Stop();
            _animationPhase = 0;
        }
    }
}
