using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PdfInspector.Viewer;

namespace AnoPDF.Desktop;

public sealed class PdfAnnotationLayerCanvas : Control
{
    private bool isDrawingEnabled;
    private PenStrokeStyle strokeStyle = PenStrokeStyle.Default;

    public PdfAnnotationLayerCanvas(PdfAnnotationLayer layer)
    {
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        ClipToBounds = true;
    }

    public PdfAnnotationLayer Layer { get; }

    public bool IsDrawingEnabled
    {
        get => isDrawingEnabled;
        set
        {
            isDrawingEnabled = value;
            Cursor = value ? new Cursor(StandardCursorType.Cross) : null;
        }
    }

    public PenStrokeStyle StrokeStyle
    {
        get => strokeStyle;
        set => strokeStyle = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));

        foreach (var stroke in Layer.Strokes)
        {
            DrawStroke(context, stroke);
        }

        if (Layer.ActiveStroke is { } activeStroke)
        {
            DrawStroke(context, activeStroke);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsDrawingEnabled)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(this);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            return;
        }

        Layer.BeginStroke(StrokeStyle, ToInkPoint(pointer.Position));
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!Layer.HasActiveStroke)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(this);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            CompleteActiveStroke(e.Pointer);
            e.Handled = true;
            return;
        }

        Layer.AddPoint(ToInkPoint(pointer.Position));
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!Layer.HasActiveStroke)
        {
            return;
        }

        Layer.AddPoint(ToInkPoint(e.GetPosition(this)));
        CompleteActiveStroke(e.Pointer);
        e.Handled = true;
    }

    private void CompleteActiveStroke(IPointer pointer)
    {
        Layer.EndStroke();
        pointer.Capture(null);
        InvalidateVisual();
    }

    private static InkStrokePoint ToInkPoint(Point point)
    {
        return new InkStrokePoint(point.X, point.Y);
    }

    private static void DrawStroke(DrawingContext context, InkStroke stroke)
    {
        var brush = CreateBrush(stroke.Style.Color);
        var pen = new Avalonia.Media.Pen(brush, stroke.Style.Width);
        var radius = stroke.Style.Width / 2;

        if (stroke.Points.Count == 1)
        {
            DrawPoint(context, brush, stroke.Points[0], radius);
            return;
        }

        for (var index = 1; index < stroke.Points.Count; index++)
        {
            context.DrawLine(
                pen,
                ToPoint(stroke.Points[index - 1]),
                ToPoint(stroke.Points[index]));
        }

        DrawPoint(context, brush, stroke.Points[0], radius);
        DrawPoint(context, brush, stroke.Points[^1], radius);
    }

    private static IBrush CreateBrush(InkColor color)
    {
        return new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
    }

    private static void DrawPoint(DrawingContext context, IBrush brush, InkStrokePoint point, double radius)
    {
        context.DrawEllipse(brush, null, ToPoint(point), radius, radius);
    }

    private static Point ToPoint(InkStrokePoint point)
    {
        return new Point(point.X, point.Y);
    }
}
