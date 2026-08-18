using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FocusPace.Controls;

using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

public sealed class FluidProgressControl : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(FluidProgressControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register(
        nameof(ProgressBrush), typeof(MediaBrush), typeof(FluidProgressControl),
        new FrameworkPropertyMetadata(MediaBrushes.CornflowerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(MediaBrush), typeof(FluidProgressControl),
        new FrameworkPropertyMetadata(MediaBrushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineBrushProperty = DependencyProperty.Register(
        nameof(OutlineBrush), typeof(MediaBrush), typeof(FluidProgressControl),
        new FrameworkPropertyMetadata(MediaBrushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register(
        nameof(IsAnimated), typeof(bool), typeof(FluidProgressControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnIsAnimatedChanged));

    private readonly DispatcherTimer _animationTimer;
    private double _wavePhase;

    public FluidProgressControl()
    {
        _animationTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(75)
        };
        _animationTimer.Tick += (_, _) =>
        {
            _wavePhase += 0.24;
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

    public MediaBrush TrackBrush
    {
        get => (MediaBrush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public MediaBrush OutlineBrush
    {
        get => (MediaBrush)GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    public bool IsAnimated
    {
        get => (bool)GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var size = Math.Min(ActualWidth, ActualHeight);
        const double outlineThickness = 2;
        var radius = Math.Max(0, (size - outlineThickness) / 2);
        var center = new System.Windows.Point(ActualWidth / 2, ActualHeight / 2);
        var clip = new EllipseGeometry(center, radius, radius);
        drawingContext.DrawEllipse(TrackBrush, null, center, radius, radius);
        drawingContext.PushClip(clip);

        var progress = Math.Clamp(Progress, 0, 1);
        if (progress > 0)
        {
            DrawWave(drawingContext, center, radius, progress, ProgressBrush, _wavePhase, 4.2);
            if (ProgressBrush.CloneCurrentValue() is MediaBrush secondaryBrush)
            {
                secondaryBrush.Opacity = 0.28;
                DrawWave(drawingContext, center, radius, progress, secondaryBrush, -_wavePhase * 0.7, 2.8);
            }
        }

        drawingContext.Pop();
        var outline = new System.Windows.Media.Pen(OutlineBrush, outlineThickness);
        drawingContext.DrawEllipse(null, outline, center, radius, radius);
    }

    private static void OnIsAnimatedChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var control = (FluidProgressControl)element;
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
            _wavePhase = 0;
        }
    }

    private static void DrawWave(
        DrawingContext drawingContext,
        System.Windows.Point center,
        double radius,
        double progress,
        MediaBrush brush,
        double phase,
        double amplitude)
    {
        var left = center.X - radius;
        var right = center.X + radius;
        var top = center.Y - radius;
        var bottom = center.Y + radius;
        var waterLine = bottom - progress * radius * 2;
        var effectiveAmplitude = progress is < 0.03 or > 0.97 ? 0 : amplitude;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new System.Windows.Point(left, bottom), true, true);
            context.LineTo(new System.Windows.Point(left, waterLine), true, false);
            const int segments = 32;
            for (var index = 0; index <= segments; index++)
            {
                var fraction = (double)index / segments;
                var x = left + fraction * radius * 2;
                var y = waterLine + Math.Sin(fraction * Math.PI * 2 + phase) * effectiveAmplitude;
                context.LineTo(new System.Windows.Point(x, y), true, false);
            }

            context.LineTo(new System.Windows.Point(right, bottom), true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(brush, null, geometry);
    }
}
