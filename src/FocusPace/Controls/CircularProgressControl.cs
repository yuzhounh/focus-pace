using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FocusPace.Controls;

using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

public sealed class CircularProgressControl : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(CircularProgressControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register(
        nameof(ProgressBrush), typeof(MediaBrush), typeof(CircularProgressControl),
        new FrameworkPropertyMetadata(MediaBrushes.CornflowerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(MediaBrush), typeof(CircularProgressControl),
        new FrameworkPropertyMetadata(MediaBrushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(CircularProgressControl),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register(
        nameof(IsAnimated), typeof(bool), typeof(CircularProgressControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnIsAnimatedChanged));

    private static readonly SolidColorBrush HighlightBrush = CreateHighlightBrush();
    private readonly DispatcherTimer _animationTimer;
    private double _animationPhase;

    public CircularProgressControl()
    {
        _animationTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(45)
        };
        _animationTimer.Tick += (_, _) =>
        {
            _animationPhase = (_animationPhase + 0.018) % 1;
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

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public bool IsAnimated
    {
        get => (bool)GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var thickness = Math.Max(1, StrokeThickness);
        var size = Math.Min(ActualWidth, ActualHeight);
        var radius = Math.Max(0, (size - thickness) / 2);
        var center = new System.Windows.Point(ActualWidth / 2, ActualHeight / 2);
        var trackPen = new System.Windows.Media.Pen(TrackBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        drawingContext.DrawEllipse(null, trackPen, center, radius, radius);

        var progress = Math.Clamp(Progress, 0, 1);
        if (progress <= 0)
        {
            return;
        }

        var progressPen = new System.Windows.Media.Pen(ProgressBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (progress >= 0.9999)
        {
            drawingContext.DrawEllipse(null, progressPen, center, radius, radius);
        }
        else
        {
            DrawArc(drawingContext, progressPen, center, radius, -90, progress * 360);
        }

        if (IsAnimated && SystemParameters.ClientAreaAnimation && progress > 0.025)
        {
            var totalSweep = progress * 360;
            var highlightSweep = Math.Min(34, Math.Max(10, totalSweep * 0.32));
            var highlightStart = -90 + Math.Max(0, totalSweep - highlightSweep) * _animationPhase;
            var highlightPen = new System.Windows.Media.Pen(HighlightBrush, Math.Max(1.5, thickness * 0.34))
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            DrawArc(drawingContext, highlightPen, center, radius, highlightStart, highlightSweep);
        }
    }

    private static void DrawArc(DrawingContext drawingContext, System.Windows.Media.Pen pen, System.Windows.Point center, double radius, double startAngle, double sweep)
    {
        if (sweep >= 359.999)
        {
            drawingContext.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, startAngle + sweep);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new System.Windows.Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise, true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);
    }

    private static void OnIsAnimatedChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var control = (CircularProgressControl)element;
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

    private static SolidColorBrush CreateHighlightBrush()
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(118, 255, 255, 255));
        brush.Freeze();
        return brush;
    }

    private static System.Windows.Point PointOnCircle(System.Windows.Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new System.Windows.Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }
}
