namespace PdfInspector.Rendering;

public sealed class RenderedPdfPage
{
    public RenderedPdfPage(
        int pageNumber,
        int pixelWidth,
        int pixelHeight,
        int stride,
        double dpi,
        byte[] pixels)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number is one-based.");
        }

        if (pixelWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), "Pixel width must be greater than zero.");
        }

        if (pixelHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Pixel height must be greater than zero.");
        }

        if (stride < pixelWidth * 4)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), "Stride must contain at least four bytes per pixel.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(pixels);

        var expectedLength = checked(stride * pixelHeight);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException("Pixel buffer length must equal stride multiplied by pixel height.", nameof(pixels));
        }

        PageNumber = pageNumber;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Stride = stride;
        Dpi = dpi;
        Pixels = pixels;
    }

    public int PageNumber { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public int Stride { get; }

    public double Dpi { get; }

    public byte[] Pixels { get; }
}
