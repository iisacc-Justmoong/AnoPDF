using PdfInspector.Rendering;

namespace AnoPDF.Rendering.Tests;

public sealed class RenderedPdfPageTests
{
    [Fact]
    public void Constructor_accepts_bgra_pixel_buffer_matching_stride_and_height()
    {
        var page = new RenderedPdfPage(
            pageNumber: 1,
            pixelWidth: 2,
            pixelHeight: 3,
            stride: 8,
            dpi: 96,
            pixels: new byte[24]);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.PixelWidth);
        Assert.Equal(3, page.PixelHeight);
        Assert.Equal(8, page.Stride);
        Assert.Equal(24, page.Pixels.Length);
    }

    [Fact]
    public void Constructor_rejects_pixel_buffer_that_does_not_match_stride_and_height()
    {
        Assert.Throws<ArgumentException>(
            () => new RenderedPdfPage(
                pageNumber: 1,
                pixelWidth: 2,
                pixelHeight: 3,
                stride: 8,
                dpi: 96,
                pixels: new byte[23]));
    }
}
