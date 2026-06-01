using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AnoPDF.Desktop;

public sealed class HslRgbTriangleColorPicker : Control
{
    private const int HueSegmentCount = 24;
    private bool isSelecting;
    private Color selectedColor = Color.Parse("#2563EB");

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
        DrawRgbTriangle(context, center, innerRadius * 0.84, SelectedColor);
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

        isSelecting = TrySelectColor(pointer.Position);
        if (isSelecting)
        {
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!isSelecting)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(this);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            EndSelection(e.Pointer);
            return;
        }

        TrySelectColor(pointer.Position);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!isSelecting)
        {
            return;
        }

        TrySelectColor(e.GetPosition(this));
        EndSelection(e.Pointer);
        e.Handled = true;
    }

    private void EndSelection(IPointer pointer)
    {
        isSelecting = false;
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
            var brush = new SolidColorBrush(FromHsl(hue, saturation: 1, lightness: 0.5));

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

    private static void DrawRgbTriangle(DrawingContext context, Point center, double radius, Color selectedColor)
    {
        var top = PointOnCircle(center, radius, -90);
        var left = PointOnCircle(center, radius, 150);
        var right = PointOnCircle(center, radius, 30);
        var core = center;

        context.DrawGeometry(Brushes.White, null, CreateTriangleGeometry(top, left, right));
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb(185, 220, 38, 38)), null, CreateTriangleGeometry(core, top, left));
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb(170, 22, 163, 74)), null, CreateTriangleGeometry(core, left, right));
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb(170, 37, 99, 235)), null, CreateTriangleGeometry(core, right, top));
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(Color.Parse("#111827")), 1),
            CreateTriangleGeometry(top, left, right));
        context.DrawEllipse(
            new SolidColorBrush(selectedColor),
            new Pen(new SolidColorBrush(Color.Parse("#111827")), 1),
            center,
            3,
            3);
    }

    private bool TrySelectColor(Point position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return false;
        }

        var size = Math.Min(Bounds.Width, Bounds.Height);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var outerRadius = size * 0.46;
        var deltaX = position.X - center.X;
        var deltaY = position.Y - center.Y;
        var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

        if (distance > outerRadius)
        {
            return false;
        }

        var angle = Math.Atan2(deltaY, deltaX);
        var hue = ((angle * 180.0 / Math.PI) + 360.0) % 360.0 / 360.0;
        var saturation = Math.Clamp(distance / outerRadius, 0, 1);

        SelectedColor = FromHsl(hue, saturation, lightness: 0.5);
        return true;
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

    private static Color FromHsl(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        var huePrime = hue * 6;
        var x = chroma * (1 - Math.Abs((huePrime % 2) - 1));
        var match = lightness - (chroma / 2);

        var (red, green, blue) = huePrime switch
        {
            >= 0 and < 1 => (chroma, x, 0.0),
            >= 1 and < 2 => (x, chroma, 0.0),
            >= 2 and < 3 => (0.0, chroma, x),
            >= 3 and < 4 => (0.0, x, chroma),
            >= 4 and < 5 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x)
        };

        return Color.FromRgb(
            ToByte(red + match),
            ToByte(green + match),
            ToByte(blue + match));
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(Math.Round(value * 255), byte.MinValue, byte.MaxValue);
    }
}
