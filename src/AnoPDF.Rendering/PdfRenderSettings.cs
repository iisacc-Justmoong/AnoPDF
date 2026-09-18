namespace PdfInspector.Rendering;

public sealed class PdfRenderSettings
{
    public PdfRenderSettings(double scale = 1.0, double dpi = 96)
    {
        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Render scale must be greater than zero.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), "Render DPI must be greater than zero.");
        }

        Scale = scale;
        Dpi = dpi;
    }

    public double Scale { get; }

    public double Dpi { get; }

    public static PdfRenderSettings Default { get; } = new();
}
