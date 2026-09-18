using Avalonia.Media;

namespace AnoPDF.Desktop;

public readonly record struct HslTriangleWeights(double PureHue, double White, double Black)
{
    public HslTriangleWeights Normalize()
    {
        if (!double.IsFinite(PureHue) || !double.IsFinite(White) || !double.IsFinite(Black))
        {
            throw new ArgumentOutOfRangeException(nameof(HslTriangleWeights), "HSL triangle weights must be finite.");
        }

        if (PureHue < 0 || White < 0 || Black < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HslTriangleWeights), "HSL triangle weights cannot be negative.");
        }

        var total = PureHue + White + Black;
        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HslTriangleWeights), "HSL triangle weights must contain color.");
        }

        return new HslTriangleWeights(PureHue / total, White / total, Black / total);
    }
}

public static class HslTriangleColorSpace
{
    public static Color FromWeights(double hue, HslTriangleWeights weights)
    {
        var normalized = weights.Normalize();
        var saturation = normalized.PureHue;
        var lightness = (normalized.PureHue * 0.5) + normalized.White;

        return FromHsl(hue, saturation, lightness);
    }

    public static Color FromHsl(double hue, double saturation, double lightness)
    {
        hue = NormalizeHue(hue);
        saturation = Math.Clamp(saturation, 0, 1);
        lightness = Math.Clamp(lightness, 0, 1);

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

    private static double NormalizeHue(double hue)
    {
        if (!double.IsFinite(hue))
        {
            throw new ArgumentOutOfRangeException(nameof(hue), "Hue must be finite.");
        }

        return ((hue % 1) + 1) % 1;
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(Math.Round(value * 255), byte.MinValue, byte.MaxValue);
    }
}
