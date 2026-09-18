using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AnoPDF.Desktop;

public sealed class HslRgbTriangleColorPicker : Control
{
    private const int HueSegmentCount = 24;
    private ColorSelectionMode colorSelectionMode;
    private double selectedHue = 220.0 / 360.0;
    private HslTriangleWeights selectedTriangleWeights = new(PureHue: 0.83, White: 0.12, Black: 0.05);
    private Color selectedColor = HslTriangleColorSpace.FromWeights(
        220.0 / 360.0,
        new HslTriangleWeights(PureHue: 0.83, White: 0.12, Black: 0.05));

    public event EventHandler<Color>? SelectedColorChanged;

    public Color SelectedColor
    {
        get => selectedColor;
        private set
        {
            if (selectedColor == value)
            {
                return;
            }

            selectedColor = value;
            SelectedColorChanged?.Invoke(this, value);
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var size = Math.Min(Bounds.Width, Bounds.Height);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var outerRadius = size * 0.46;
        var innerRadius = outerRadius * 0.66;

        DrawHueWheel(context, center, innerRadius, outerRadius);
        DrawHslTriangle(context, center, innerRadius * 0.84, selectedHue, selectedTriangleWeights);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(96, 72);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pointer = e.GetCurrentPoint(this);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            return;
        }

        colorSelectionMode = GetSelectionMode(pointer.Position);
        if (colorSelectionMode is not ColorSelectionMode.None)
        {
            TrySelectColor(pointer.Position, colorSelectionMode);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (colorSelectionMode is ColorSelectionMode.None)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(this);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            EndSelection(e.Pointer);
            return;
        }

        TrySelectColor(pointer.Position, colorSelectionMode);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (colorSelectionMode is ColorSelectionMode.None)
        {
            return;
        }

        TrySelectColor(e.GetPosition(this), colorSelectionMode);
        EndSelection(e.Pointer);
        e.Handled = true;
    }

    private void EndSelection(IPointer pointer)
    {
        colorSelectionMode = ColorSelectionMode.None;
        pointer.Capture(null);
    }

    private static void DrawHueWheel(
        DrawingContext context,
        Point center,
        double innerRadius,
        double outerRadius)
    {
        for (var index = 0; index < HueSegmentCount; index++)
        {
            var startAngle = index * 360.0 / HueSegmentCount;
            var endAngle = (index + 1) * 360.0 / HueSegmentCount;
            var hue = (startAngle + endAngle) / 720.0;
            var brush = new SolidColorBrush(HslTriangleColorSpace.FromHsl(hue, saturation: 1, lightness: 0.5));

            context.DrawGeometry(
                brush,
                null,
                CreateRingSegmentGeometry(center, innerRadius, outerRadius, startAngle, endAngle));
        }

        context.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(Color.Parse("#1F2937")), 1),
            center,
            outerRadius,
            outerRadius);
    }

    private static void DrawHslTriangle(
        DrawingContext context,
        Point center,
        double radius,
        double hue,
        HslTriangleWeights selectedWeights)
    {
        var top = PointOnCircle(center, radius, -90);
        var left = PointOnCircle(center, radius, 150);
        var right = PointOnCircle(center, radius, 30);

        DrawHslTriangleFill(context, top, left, right, hue);
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(Color.Parse("#111827")), 1),
            CreateTriangleGeometry(top, left, right));

        var marker = FromWeights(top, left, right, selectedWeights);
        context.DrawEllipse(
            new SolidColorBrush(HslTriangleColorSpace.FromWeights(hue, selectedWeights)),
            new Pen(new SolidColorBrush(Color.Parse("#111827")), 1),
            marker,
            3,
            3);
    }

    private static void DrawHslTriangleFill(
        DrawingContext context,
        Point top,
        Point left,
        Point right,
        double hue)
    {
        var minX = (int)Math.Floor(Math.Min(top.X, Math.Min(left.X, right.X)));
        var maxX = (int)Math.Ceiling(Math.Max(top.X, Math.Max(left.X, right.X)));
        var minY = (int)Math.Floor(Math.Min(top.Y, Math.Min(left.Y, right.Y)));
        var maxY = (int)Math.Ceiling(Math.Max(top.Y, Math.Max(left.Y, right.Y)));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var sample = new Point(x + 0.5, y + 0.5);
                if (!TryGetWeights(sample, top, left, right, out var weights))
                {
                    continue;
                }

                context.DrawRectangle(
                    new SolidColorBrush(HslTriangleColorSpace.FromWeights(hue, weights)),
                    null,
                    new Rect(x, y, 1, 1));
            }
        }
    }

    private ColorSelectionMode GetSelectionMode(Point position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return ColorSelectionMode.None;
        }

        var size = Math.Min(Bounds.Width, Bounds.Height);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var outerRadius = size * 0.46;
        var innerRadius = outerRadius * 0.66;
        var triangleRadius = innerRadius * 0.84;
        var top = PointOnCircle(center, triangleRadius, -90);
        var left = PointOnCircle(center, triangleRadius, 150);
        var right = PointOnCircle(center, triangleRadius, 30);

        if (TryGetWeights(position, top, left, right, out _))
        {
            return ColorSelectionMode.HslTriangle;
        }

        var deltaX = position.X - center.X;
        var deltaY = position.Y - center.Y;
        var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

        return distance >= innerRadius && distance <= outerRadius
            ? ColorSelectionMode.HueRing
            : ColorSelectionMode.None;
    }

    private bool TrySelectColor(Point position, ColorSelectionMode selectionMode)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return false;
        }

        var size = Math.Min(Bounds.Width, Bounds.Height);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var outerRadius = size * 0.46;
        var innerRadius = outerRadius * 0.66;
        var triangleRadius = innerRadius * 0.84;

        if (selectionMode is ColorSelectionMode.HslTriangle)
        {
            var top = PointOnCircle(center, triangleRadius, -90);
            var left = PointOnCircle(center, triangleRadius, 150);
            var right = PointOnCircle(center, triangleRadius, 30);
            if (!TryGetWeights(position, top, left, right, out var weights))
            {
                return false;
            }

            selectedTriangleWeights = weights;
            SelectedColor = HslTriangleColorSpace.FromWeights(selectedHue, selectedTriangleWeights);
            return true;
        }

        var deltaX = position.X - center.X;
        var deltaY = position.Y - center.Y;
        var angle = Math.Atan2(deltaY, deltaX);
        selectedHue = ((angle * 180.0 / Math.PI) + 360.0) % 360.0 / 360.0;
        SelectedColor = HslTriangleColorSpace.FromWeights(selectedHue, selectedTriangleWeights);
        return true;
    }

    private static bool TryGetWeights(
        Point point,
        Point pureHue,
        Point white,
        Point black,
        out HslTriangleWeights weights)
    {
        var denominator =
            ((white.Y - black.Y) * (pureHue.X - black.X)) +
            ((black.X - white.X) * (pureHue.Y - black.Y));

        if (Math.Abs(denominator) < double.Epsilon)
        {
            weights = default;
            return false;
        }

        var pureHueWeight =
            (((white.Y - black.Y) * (point.X - black.X)) +
             ((black.X - white.X) * (point.Y - black.Y))) / denominator;
        var whiteWeight =
            (((black.Y - pureHue.Y) * (point.X - black.X)) +
             ((pureHue.X - black.X) * (point.Y - black.Y))) / denominator;
        var blackWeight = 1 - pureHueWeight - whiteWeight;

        const double Tolerance = 0.0001;
        if (pureHueWeight < -Tolerance || whiteWeight < -Tolerance || blackWeight < -Tolerance)
        {
            weights = default;
            return false;
        }

        weights = new HslTriangleWeights(
            Math.Clamp(pureHueWeight, 0, 1),
            Math.Clamp(whiteWeight, 0, 1),
            Math.Clamp(blackWeight, 0, 1));
        return true;
    }

    private static Point FromWeights(
        Point pureHue,
        Point white,
        Point black,
        HslTriangleWeights weights)
    {
        var normalized = weights.Normalize();
        return new Point(
            (pureHue.X * normalized.PureHue) + (white.X * normalized.White) + (black.X * normalized.Black),
            (pureHue.Y * normalized.PureHue) + (white.Y * normalized.White) + (black.Y * normalized.Black));
    }

    private static StreamGeometry CreateRingSegmentGeometry(
        Point center,
        double innerRadius,
        double outerRadius,
        double startAngle,
        double endAngle)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        var outerStart = PointOnCircle(center, outerRadius, startAngle);
        var outerEnd = PointOnCircle(center, outerRadius, endAngle);
        var innerEnd = PointOnCircle(center, innerRadius, endAngle);
        var innerStart = PointOnCircle(center, innerRadius, startAngle);

        context.BeginFigure(outerStart, isFilled: true);
        context.LineTo(outerEnd);
        context.LineTo(innerEnd);
        context.LineTo(innerStart);
        context.EndFigure(isClosed: true);

        return geometry;
    }

    private static StreamGeometry CreateTriangleGeometry(Point first, Point second, Point third)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        context.BeginFigure(first, isFilled: true);
        context.LineTo(second);
        context.LineTo(third);
        context.EndFigure(isClosed: true);

        return geometry;
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var angleRadians = Math.PI * angleDegrees / 180.0;
        return new Point(
            center.X + (Math.Cos(angleRadians) * radius),
            center.Y + (Math.Sin(angleRadians) * radius));
    }

    private enum ColorSelectionMode
    {
        None,
        HueRing,
        HslTriangle
    }
}
