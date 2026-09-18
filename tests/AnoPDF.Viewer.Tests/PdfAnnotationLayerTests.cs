using PdfInspector.Viewer;

namespace AnoPDF.Viewer.Tests;

public sealed class PdfAnnotationLayerTests
{
    [Fact]
    public void Pen_stroke_style_captures_width_and_color_parameters()
    {
        var style = new PenStrokeStyle(
            width: 6,
            color: new InkColor(255, 12, 34, 56));

        Assert.Equal(6, style.Width);
        Assert.Equal(new InkColor(255, 12, 34, 56), style.Color);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Pen_stroke_style_rejects_non_drawable_widths(double width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PenStrokeStyle(width, InkColor.FromRgb(37, 99, 235)));
    }

    [Fact]
    public void Annotation_layer_commits_mouse_stroke_separately_from_the_document()
    {
        var layer = new PdfAnnotationLayer();
        var style = new PenStrokeStyle(4, InkColor.FromRgb(37, 99, 235));

        layer.BeginStroke(style, new InkStrokePoint(10, 20));
        layer.AddPoint(new InkStrokePoint(30, 40));

        Assert.Empty(layer.Strokes);
        Assert.NotNull(layer.ActiveStroke);

        var stroke = layer.EndStroke();

        Assert.NotNull(stroke);
        Assert.Single(layer.Strokes);
        Assert.Same(stroke, layer.Strokes[0]);
        Assert.Equal(style.Width, stroke.Style.Width);
        Assert.Equal(style.Color, stroke.Style.Color);
        Assert.Equal(
            [new InkStrokePoint(10, 20), new InkStrokePoint(30, 40)],
            stroke.Points);
        Assert.Null(layer.ActiveStroke);
    }

    [Fact]
    public void Annotation_layer_discards_active_stroke_without_committing_it()
    {
        var layer = new PdfAnnotationLayer();

        layer.BeginStroke(PenStrokeStyle.Default, new InkStrokePoint(5, 6));
        layer.CancelStroke();

        Assert.Empty(layer.Strokes);
        Assert.Null(layer.ActiveStroke);
    }
}
