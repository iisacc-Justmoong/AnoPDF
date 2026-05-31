namespace PdfInspector.Rendering;

public static class PdfRenderGeometry
{
    private const double PdfPointsPerInch = 72.0;

    public static RenderedPdfSize CalculatePixelSize(
        double pageWidthPoints,
        double pageHeightPoints,
        PdfRenderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (pageWidthPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageWidthPoints), "Page width must be greater than zero.");
        }

        if (pageHeightPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageHeightPoints), "Page height must be greater than zero.");
        }

        var width = ConvertPointsToPixels(pageWidthPoints, settings);
        var height = ConvertPointsToPixels(pageHeightPoints, settings);
        return new RenderedPdfSize(width, height);
    }

    private static int ConvertPointsToPixels(double points, PdfRenderSettings settings)
    {
        var pixels = points / PdfPointsPerInch * settings.Dpi * settings.Scale;
        return Math.Max(1, (int)Math.Round(pixels, MidpointRounding.AwayFromZero));
    }
}
