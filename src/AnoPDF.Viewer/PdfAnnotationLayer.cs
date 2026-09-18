namespace PdfInspector.Viewer;

public readonly record struct InkColor(byte Alpha, byte Red, byte Green, byte Blue)
{
    public static InkColor FromRgb(byte red, byte green, byte blue)
    {
        return new InkColor(byte.MaxValue, red, green, blue);
    }
}

public readonly record struct InkStrokePoint
{
    public InkStrokePoint(double x, double y)
    {
        X = ValidateCoordinate(x, nameof(x));
        Y = ValidateCoordinate(y, nameof(y));
    }

    public double X { get; }

    public double Y { get; }

    private static double ValidateCoordinate(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Ink stroke coordinates must be finite.");
        }

        return value;
    }
}

public sealed class PenStrokeStyle
{
    public PenStrokeStyle(double width, InkColor color)
    {
        if (!double.IsFinite(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Pen stroke width must be greater than zero.");
        }

        Width = width;
        Color = color;
    }

    public static PenStrokeStyle Default { get; } = new(3, InkColor.FromRgb(37, 99, 235));

    public double Width { get; }

    public InkColor Color { get; }
}

public sealed class InkStroke
{
    public InkStroke(PenStrokeStyle style, IEnumerable<InkStrokePoint> points)
    {
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(points);

        var copiedPoints = points.ToArray();
        if (copiedPoints.Length == 0)
        {
            throw new ArgumentException("An ink stroke must contain at least one point.", nameof(points));
        }

        Style = style;
        Points = Array.AsReadOnly(copiedPoints);
    }

    public PenStrokeStyle Style { get; }

    public IReadOnlyList<InkStrokePoint> Points { get; }
}

public sealed class PdfAnnotationLayer
{
    private readonly List<InkStroke> strokes = [];
    private InkStrokeBuilder? activeStroke;

    public IReadOnlyList<InkStroke> Strokes => strokes;

    public bool HasActiveStroke => activeStroke is not null;

    public InkStroke? ActiveStroke => activeStroke?.ToStroke();

    public void BeginStroke(PenStrokeStyle style, InkStrokePoint point)
    {
        if (activeStroke is not null)
        {
            throw new InvalidOperationException("Only one active ink stroke can be edited at a time.");
        }

        activeStroke = new InkStrokeBuilder(style);
        activeStroke.AddPoint(point);
    }

    public void AddPoint(InkStrokePoint point)
    {
        if (activeStroke is null)
        {
            throw new InvalidOperationException("A stroke must be started before adding points.");
        }

        activeStroke.AddPoint(point);
    }

    public InkStroke? EndStroke()
    {
        if (activeStroke is null)
        {
            return null;
        }

        var stroke = activeStroke.ToStroke();
        strokes.Add(stroke);
        activeStroke = null;

        return stroke;
    }

    public void CancelStroke()
    {
        activeStroke = null;
    }

    private sealed class InkStrokeBuilder
    {
        private readonly List<InkStrokePoint> points = [];

        public InkStrokeBuilder(PenStrokeStyle style)
        {
            Style = style ?? throw new ArgumentNullException(nameof(style));
        }

        private PenStrokeStyle Style { get; }

        public void AddPoint(InkStrokePoint point)
        {
            points.Add(point);
        }

        public InkStroke ToStroke()
        {
            return new InkStroke(Style, points);
        }
    }
}
