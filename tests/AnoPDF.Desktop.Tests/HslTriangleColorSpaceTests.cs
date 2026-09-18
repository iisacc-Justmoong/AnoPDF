using AnoPDF.Desktop;

namespace AnoPDF.Desktop.Tests;

public sealed class HslTriangleColorSpaceTests
{
    [Fact]
    public void Pure_hue_vertex_uses_the_selected_hue()
    {
        var color = HslTriangleColorSpace.FromWeights(
            hue: 0,
            new HslTriangleWeights(PureHue: 1, White: 0, Black: 0));

        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void White_and_black_vertices_remain_neutral_for_any_hue()
    {
        var white = HslTriangleColorSpace.FromWeights(
            hue: 0.42,
            new HslTriangleWeights(PureHue: 0, White: 1, Black: 0));
        var black = HslTriangleColorSpace.FromWeights(
            hue: 0.42,
            new HslTriangleWeights(PureHue: 0, White: 0, Black: 1));

        Assert.Equal(255, white.R);
        Assert.Equal(255, white.G);
        Assert.Equal(255, white.B);
        Assert.Equal(0, black.R);
        Assert.Equal(0, black.G);
        Assert.Equal(0, black.B);
    }

    [Fact]
    public void Mixed_triangle_position_maps_to_hsl_saturation_and_lightness()
    {
        var color = HslTriangleColorSpace.FromWeights(
            hue: 0,
            new HslTriangleWeights(PureHue: 0.5, White: 0.5, Black: 0));

        Assert.Equal(223, color.R);
        Assert.Equal(159, color.G);
        Assert.Equal(159, color.B);
    }
}
