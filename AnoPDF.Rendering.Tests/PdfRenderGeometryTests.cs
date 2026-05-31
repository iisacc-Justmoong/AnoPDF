using PdfInspector.Rendering;

namespace AnoPDF.Rendering.Tests;

public sealed class PdfRenderGeometryTests
{
    [Fact]
    public void CalculatePixelSize_converts_pdf_points_to_display_pixels()
    {
        var settings = new PdfRenderSettings(scale: 1.0, dpi: 96);

        var size = PdfRenderGeometry.CalculatePixelSize(612, 792, settings);

        Assert.Equal(816, size.Width);
        Assert.Equal(1056, size.Height);
    }

    [Fact]
    public void CalculatePixelSize_applies_zoom_scale()
    {
        var settings = new PdfRenderSettings(scale: 1.5, dpi: 96);

        var size = PdfRenderGeometry.CalculatePixelSize(612, 792, settings);

        Assert.Equal(1224, size.Width);
        Assert.Equal(1584, size.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Settings_rejects_non_positive_scale(double scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfRenderSettings(scale, dpi: 96));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-96)]
    public void Settings_rejects_non_positive_dpi(double dpi)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfRenderSettings(scale: 1, dpi));
    }
}
