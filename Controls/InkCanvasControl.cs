using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using NoteekFlow.Models;

namespace NoteekFlow.Controls;

public sealed class InkCanvasControl : Control
{
    public static readonly StyledProperty<ObservableCollection<InkStroke>?> StrokesProperty =
        AvaloniaProperty.Register<InkCanvasControl, ObservableCollection<InkStroke>?>(nameof(Strokes));
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<InkCanvasControl, IBrush?>(nameof(Background));
    public static readonly StyledProperty<string> ToolProperty =
        AvaloniaProperty.Register<InkCanvasControl, string>(nameof(Tool), "Pen");
    public static readonly StyledProperty<string> StrokeColorProperty =
        AvaloniaProperty.Register<InkCanvasControl, string>(nameof(StrokeColor), "#20242B");
    public static readonly StyledProperty<string> CanvasColorProperty =
        AvaloniaProperty.Register<InkCanvasControl, string>(nameof(CanvasColor), "#FBFAF7");
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<InkCanvasControl, bool>(nameof(ShowGrid));
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<InkCanvasControl, double>(nameof(StrokeWidth), 3);
    public static readonly StyledProperty<double> PanXProperty =
        AvaloniaProperty.Register<InkCanvasControl, double>(nameof(PanX));
    public static readonly StyledProperty<double> PanYProperty =
        AvaloniaProperty.Register<InkCanvasControl, double>(nameof(PanY));
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<InkCanvasControl, double>(nameof(Zoom), 1);

    private InkStroke? _activeStroke;
    private ObservableCollection<InkStroke>? _observedStrokes;
    private readonly HashSet<InkStroke> _observedElements = new();
    private readonly Dictionary<InkStroke, CachedStroke> _penGeometryCache = new();
    private readonly Dictionary<ObservableCollection<InkPoint>, InkStroke> _elementByPoints = new();
    private Point? _cursorPosition;
    private Point? _panStartPointer;
    private double _panStartX;
    private double _panStartY;

    public ObservableCollection<InkStroke>? Strokes
    {
        get => GetValue(StrokesProperty);
        set => SetValue(StrokesProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public string Tool
    {
        get => GetValue(ToolProperty);
        set => SetValue(ToolProperty, value);
    }

    public string StrokeColor
    {
        get => GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public string CanvasColor
    {
        get => GetValue(CanvasColorProperty);
        set => SetValue(CanvasColorProperty, value);
    }

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public double PanX
    {
        get => GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    public double PanY
    {
        get => GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    static InkCanvasControl()
    {
        AffectsRender<InkCanvasControl>(
            StrokesProperty,
            BackgroundProperty,
            ToolProperty,
            StrokeColorProperty,
            CanvasColorProperty,
            ShowGridProperty,
            StrokeWidthProperty,
            PanXProperty,
            PanYProperty,
            ZoomProperty);
    }

    public InkCanvasControl()
    {
        Focusable = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StrokesProperty)
        {
            if (_observedStrokes is not null)
            {
                _observedStrokes.CollectionChanged -= OnStrokesChanged;
            }

            foreach (var element in _observedElements)
            {
                element.Points.CollectionChanged -= OnElementPointsChanged;
            }

            _observedElements.Clear();
            _elementByPoints.Clear();
            _penGeometryCache.Clear();
            _observedStrokes = change.GetNewValue<ObservableCollection<InkStroke>?>();
            if (_observedStrokes is not null)
            {
                _observedStrokes.CollectionChanged += OnStrokesChanged;
                foreach (var element in _observedStrokes)
                {
                    ObserveElement(element);
                }
            }

            InvalidateVisual();
        }
        else if (change.Property == PanXProperty
            || change.Property == PanYProperty
            || change.Property == ZoomProperty
            || change.Property == CanvasColorProperty
            || change.Property == ShowGridProperty
            || change.Property == StrokeColorProperty
            || change.Property == StrokeWidthProperty
            || change.Property == ToolProperty)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(ParseColor(CanvasColor)), new Rect(Bounds.Size));
        if (ShowGrid)
        {
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(48, 90, 100, 110)), 1);
            var gridSpacing = 24 * Zoom;
            var firstX = -(PanX % gridSpacing);
            var firstY = -(PanY % gridSpacing);
            if (firstX > 0)
            {
                firstX -= gridSpacing;
            }

            if (firstY > 0)
            {
                firstY -= gridSpacing;
            }

            for (var x = firstX; x <= Bounds.Width; x += gridSpacing)
            {
                context.DrawLine(gridPen, new Point(x, 0), new Point(x, Bounds.Height));
            }

            for (var y = firstY; y <= Bounds.Height; y += gridSpacing)
            {
                context.DrawLine(gridPen, new Point(0, y), new Point(Bounds.Width, y));
            }
        }

        using (context.PushTransform(new Matrix(Zoom, 0, 0, Zoom, -PanX, -PanY)))
        {
            if (Strokes is not null)
            {
                foreach (var stroke in Strokes)
                {
                    if (stroke.Points.Count == 0)
                    {
                        continue;
                    }

                    var brush = new SolidColorBrush(ParseColor(stroke.Color));
                    if (stroke.Kind == "Text")
                    {
                        var text = new FormattedText(
                            stroke.Text ?? string.Empty,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Arial"),
                            18,
                            brush);
                        context.DrawText(text, new Point(stroke.Points[0].X, stroke.Points[0].Y));
                        continue;
                    }

                    if (stroke.Kind is "Rectangle" or "Ellipse" or "Line")
                    {
                        var start = new Point(stroke.Points[0].X, stroke.Points[0].Y);
                        var endPoint = stroke.Points[^1];
                        var end = new Point(endPoint.X, endPoint.Y);
                        var pen = new Pen(brush, stroke.Width);
                        if (stroke.Kind == "Line")
                        {
                            context.DrawLine(pen, start, end);
                        }
                        else
                        {
                            var shapeRect = new Rect(start, end).Normalize();
                            if (stroke.Kind == "Rectangle")
                            {
                                context.DrawRectangle(null, pen, shapeRect);
                            }
                            else
                            {
                                context.DrawEllipse(null, pen, shapeRect.Center, shapeRect.Width / 2, shapeRect.Height / 2);
                            }
                        }

                        continue;
                    }

                    if (stroke.Points.Count == 1)
                    {
                        var point = stroke.Points[0];
                        var radius = PointWidth(point.Pressure, stroke.Width) / 2;
                        context.DrawEllipse(brush, null, new Point(point.X, point.Y), radius, radius);
                        continue;
                    }

                    if (!_penGeometryCache.TryGetValue(stroke, out var cachedStroke))
                    {
                        cachedStroke = CreateStrokeGeometry(stroke);
                        _penGeometryCache.Add(stroke, cachedStroke);
                    }

                    var visibleWorldBounds = new Rect(
                        PanX / Zoom,
                        PanY / Zoom,
                        Bounds.Width / Zoom,
                        Bounds.Height / Zoom);
                    if (cachedStroke.Bounds.Intersects(visibleWorldBounds))
                    {
                        var width = PointWidth((float)cachedStroke.AveragePressure, stroke.Width);
                        context.DrawGeometry(null, new Pen(brush, width), cachedStroke.Geometry);
                    }
                }
            }
        }

        if (_cursorPosition is { } cursor && IsEnabled)
        {
            var previewColor = ParseColor(StrokeColor);
            var previewBrush = new SolidColorBrush(Color.FromArgb(90, previewColor.R, previewColor.G, previewColor.B));
            var radius = Tool == "Eraser" ? 9 : StrokeWidth * Zoom / 2 + 3;
            context.DrawEllipse(null, new Pen(previewBrush, 1.5), cursor, radius, radius);
            context.DrawEllipse(previewBrush, null, cursor, 2, 2);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _panStartPointer = e.GetPosition(this);
            _panStartX = PanX;
            _panStartY = PanY;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (Strokes is null || e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        _cursorPosition = e.GetPosition(this);
        if (Tool == "Text")
        {
            var position = e.GetPosition(this);
            TextRequested?.Invoke(new Point((position.X + PanX) / Zoom, (position.Y + PanY) / Zoom));
            e.Handled = true;
            return;
        }

        if (Tool == "Eraser")
        {
            EraseAt(e.GetPosition(this));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        _activeStroke = new InkStroke();
        _activeStroke.Kind = Tool;
        _activeStroke.Color = StrokeColor;
        _activeStroke.Width = StrokeWidth;
        AddPoint(e);
        if (Tool is "Rectangle" or "Ellipse" or "Line")
        {
            AddPoint(e);
        }

        Strokes.Add(_activeStroke);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _cursorPosition = e.GetPosition(this);
        InvalidateVisual();
        if (_panStartPointer is { } panStart)
        {
            var position = e.GetPosition(this);
            PanX = _panStartX - (position.X - panStart.X);
            PanY = _panStartY - (position.Y - panStart.Y);
            e.Handled = true;
            return;
        }

        if (Tool == "Eraser" && e.Pointer.Captured == this)
        {
            EraseAt(e.GetPosition(this));
            e.Handled = true;
            return;
        }

        if (_activeStroke is null || e.Pointer.Captured != this)
        {
            return;
        }

        if (_activeStroke.Kind is "Rectangle" or "Ellipse" or "Line")
        {
            _activeStroke.Points[^1] = GetInkPoint(e);
        }
        else
        {
            AddPoint(e);
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panStartPointer is not null)
        {
            _panStartPointer = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_activeStroke is null)
        {
            if (Tool == "Eraser" && e.Pointer.Captured == this)
            {
                EraseAt(e.GetPosition(this));
                e.Pointer.Capture(null);
                e.Handled = true;
            }

            return;
        }

        if (_activeStroke.Kind is "Rectangle" or "Ellipse" or "Line")
        {
            _activeStroke.Points[^1] = GetInkPoint(e);
        }
        else
        {
            AddPoint(e);
        }

        _activeStroke = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (e.Pointer.Captured != this)
        {
            _cursorPosition = null;
            InvalidateVisual();
        }
    }

    private void AddPoint(PointerEventArgs e)
    {
        if (_activeStroke is null)
        {
            return;
        }

        _activeStroke.Points.Add(GetInkPoint(e));
    }

    private InkPoint GetInkPoint(PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        return new InkPoint((point.Position.X + PanX) / Zoom, (point.Position.Y + PanY) / Zoom, point.Properties.Pressure);
    }

    private void EraseAt(Point position)
    {
        if (Strokes is null)
        {
            return;
        }

        for (var index = Strokes.Count - 1; index >= 0; index--)
        {
            var element = Strokes[index];
            if (IsNearElement(element, new Point((position.X + PanX) / Zoom, (position.Y + PanY) / Zoom), 14 / Zoom))
            {
                Strokes.RemoveAt(index);
                break;
            }
        }

        InvalidateVisual();
    }

    private static bool IsNearElement(InkStroke element, Point point, double tolerance)
    {
        if (element.Points.Count == 1)
        {
            if (element.Kind == "Text")
            {
                var origin = element.Points[0];
                var textWidth = Math.Max(18, (element.Text?.Length ?? 0) * 10);
                return point.X >= origin.X - tolerance
                    && point.X <= origin.X + textWidth + tolerance
                    && point.Y >= origin.Y - 22 - tolerance
                    && point.Y <= origin.Y + tolerance;
            }

            return Distance(new Point(element.Points[0].X, element.Points[0].Y), point) <= tolerance;
        }

        if (element.Points.Count == 2 && (element.Kind is "Rectangle" or "Ellipse"))
        {
            var rect = new Rect(
                new Point(element.Points[0].X, element.Points[0].Y),
                new Point(element.Points[1].X, element.Points[1].Y)).Normalize();
            if (element.Kind == "Rectangle")
            {
                return DistanceToSegment(point, new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Top)) <= tolerance
                    || DistanceToSegment(point, new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom)) <= tolerance
                    || DistanceToSegment(point, new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Bottom)) <= tolerance
                    || DistanceToSegment(point, new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Top)) <= tolerance;
            }

            var radiusX = rect.Width / 2;
            var radiusY = rect.Height / 2;
            if (radiusX == 0 || radiusY == 0)
            {
                return false;
            }

            var normalizedX = (point.X - rect.Center.X) / radiusX;
            var normalizedY = (point.Y - rect.Center.Y) / radiusY;
            return Math.Abs(Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY) - 1)
                * Math.Min(radiusX, radiusY) <= tolerance;
        }

        for (var i = 1; i < element.Points.Count; i++)
        {
            var previous = new Point(element.Points[i - 1].X, element.Points[i - 1].Y);
            var current = new Point(element.Points[i].X, element.Points[i].Y);
            if (DistanceToSegment(point, previous, current) <= tolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static double DistanceToSegment(Point point, Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if (dx == 0 && dy == 0)
        {
            return Distance(point, start);
        }

        var projection = Math.Clamp(
            ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / (dx * dx + dy * dy),
            0,
            1);
        return Distance(point, new Point(start.X + projection * dx, start.Y + projection * dy));
    }

    private static double Distance(Point first, Point second) =>
        Math.Sqrt(Math.Pow(first.X - second.X, 2) + Math.Pow(first.Y - second.Y, 2));

    private static double PointWidth(float pressure, double width) =>
        Math.Max(1, width * Math.Clamp(pressure, 0.1f, 1f));

    private static Color ParseColor(string color)
    {
        try
        {
            return Color.Parse(color);
        }
        catch (FormatException)
        {
            return Color.Parse("#20242B");
        }
    }

    private void OnElementPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is ObservableCollection<InkPoint> points
            && _elementByPoints.TryGetValue(points, out var element))
        {
            _penGeometryCache.Remove(element);
        }

        InvalidateVisual();
    }

    private void ObserveElement(InkStroke element)
    {
        if (_observedElements.Add(element))
        {
            _elementByPoints[element.Points] = element;
            element.Points.CollectionChanged += OnElementPointsChanged;
        }
    }

    private void UnobserveElement(InkStroke element)
    {
        if (_observedElements.Remove(element))
        {
            element.Points.CollectionChanged -= OnElementPointsChanged;
            _elementByPoints.Remove(element.Points);
            _penGeometryCache.Remove(element);
        }
    }

    private void OnStrokesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var element in _observedElements)
            {
                element.Points.CollectionChanged -= OnElementPointsChanged;
            }

            _observedElements.Clear();
            _elementByPoints.Clear();
            _penGeometryCache.Clear();
            if (_observedStrokes is not null)
            {
                foreach (var element in _observedStrokes)
                {
                    ObserveElement(element);
                }
            }
        }
        else
        {
            if (e.OldItems is not null)
            {
                foreach (InkStroke element in e.OldItems)
                {
                    UnobserveElement(element);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (InkStroke element in e.NewItems)
                {
                    ObserveElement(element);
                }
            }
        }

        InvalidateVisual();
    }

    private static CachedStroke CreateStrokeGeometry(InkStroke stroke)
    {
        var geometry = new StreamGeometry();
        var firstPoint = stroke.Points[0];
        var left = firstPoint.X;
        var right = firstPoint.X;
        var top = firstPoint.Y;
        var bottom = firstPoint.Y;
        var pressureSum = firstPoint.Pressure;
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(firstPoint.X, firstPoint.Y), isFilled: false);
            for (var index = 1; index < stroke.Points.Count; index++)
            {
                var point = stroke.Points[index];
                geometryContext.LineTo(new Point(point.X, point.Y));
                left = Math.Min(left, point.X);
                right = Math.Max(right, point.X);
                top = Math.Min(top, point.Y);
                bottom = Math.Max(bottom, point.Y);
                pressureSum += point.Pressure;
            }

            geometryContext.EndFigure(isClosed: false);
        }

        var padding = stroke.Width;
        var bounds = new Rect(left - padding, top - padding, right - left + padding * 2, bottom - top + padding * 2);
        return new CachedStroke(geometry, bounds, pressureSum / stroke.Points.Count);
    }

    private sealed record CachedStroke(StreamGeometry Geometry, Rect Bounds, double AveragePressure);

    public event Action<Point>? TextRequested;
}
