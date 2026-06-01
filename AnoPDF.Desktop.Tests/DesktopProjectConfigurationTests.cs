using System.Xml.Linq;

namespace AnoPDF.Desktop.Tests;

public sealed class DesktopProjectConfigurationTests
{
    [Fact]
    public void Desktop_project_targets_cross_platform_dotnet_runtime()
    {
        var project = LoadDesktopProject();

        Assert.Equal("net9.0", GetProperty(project, "TargetFramework"));
        Assert.Null(GetProperty(project, "UseWPF"));
        Assert.Null(GetProperty(project, "EnableWindowsTargeting"));
    }

    [Fact]
    public void Desktop_project_uses_avalonia_instead_of_windows_desktop_runtime()
    {
        var project = LoadDesktopProject();
        var packageNames = project
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        Assert.Contains("Avalonia.Desktop", packageNames);
        Assert.DoesNotContain("Microsoft.WindowsDesktop.App", packageNames);
    }

    [Fact]
    public void Desktop_window_presents_pdf_pages_as_a_scrollable_document_stack()
    {
        var markup = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml"));

        Assert.Contains("ScrollViewer", markup, StringComparison.Ordinal);
        Assert.Contains("PageStack", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviousPageButton", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("NextPageButton", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_window_opens_pdf_dialog_from_the_attached_top_level()
    {
        var code = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml.cs"));

        Assert.Contains("TopLevel.GetTopLevel(this)", code, StringComparison.Ordinal);
        Assert.Contains("storageProvider.CanOpen", code, StringComparison.Ordinal);
        Assert.Contains("OpenFilePickerAsync", code, StringComparison.Ordinal);
        Assert.Contains("TryGetLocalPath()", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_start_view_has_a_direct_file_dialog_button_below_the_path_text()
    {
        var markup = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml"));
        var pathTextIndex = markup.IndexOf("SelectedPathText", StringComparison.Ordinal);
        var directButtonIndex = markup.IndexOf("DirectFileDialogButton", StringComparison.Ordinal);

        Assert.True(pathTextIndex >= 0);
        Assert.True(directButtonIndex > pathTextIndex);
        Assert.Contains("Content=\"Open\"", markup, StringComparison.Ordinal);
        Assert.Contains("Click=\"OpenButton_Click\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("선택된 파일 없음", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("파일 다이얼로그 열기", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_window_uses_high_contrast_modern_layout_chrome()
    {
        var markup = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml"));

        Assert.Contains("Background=\"#E5E7EB\"", markup, StringComparison.Ordinal);
        Assert.Contains("StartPanel", markup, StringComparison.Ordinal);
        Assert.Contains("ViewerTopBar", markup, StringComparison.Ordinal);
        Assert.Contains("Background=\"#0F172A\"", markup, StringComparison.Ordinal);
        Assert.Contains("Background=\"#0F62FE\"", markup, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"#FFFFFF\"", markup, StringComparison.Ordinal);
        Assert.True(GetContrastRatio("#0F62FE", "#FFFFFF") >= 4.5);
        Assert.True(GetContrastRatio("#0F172A", "#F8FAFC") >= 7);
    }

    [Fact]
    public void Desktop_window_has_a_bottom_drawing_toolbar_with_basic_tools()
    {
        var markup = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml"));
        var code = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "HslRgbTriangleColorPicker.cs"));

        Assert.Contains("DrawingToolbar", markup, StringComparison.Ordinal);
        Assert.Contains("PanToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("Click=\"PanToolButton_Click\"", markup, StringComparison.Ordinal);
        Assert.Contains("PenToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("Click=\"PenToolButton_Click\"", markup, StringComparison.Ordinal);
        Assert.Contains("HighlighterToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("EraserToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("HslRgbTriangleColorPicker", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawingColorBlackButton", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawingColorRedButton", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawingColorBlueButton", markup, StringComparison.Ordinal);
        Assert.Contains("StrokeWidthSlider", markup, StringComparison.Ordinal);
        Assert.Contains("ValueChanged=\"StrokeWidthSlider_ValueChanged\"", markup, StringComparison.Ordinal);
        Assert.Contains("DrawHslTriangle", code, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawRgbTriangle", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_window_places_one_annotation_layer_over_each_rendered_pdf_page()
    {
        var code = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml.cs"));

        Assert.Contains("new PdfAnnotationLayerCanvas", code, StringComparison.Ordinal);
        Assert.Contains("new PdfAnnotationLayer()", code, StringComparison.Ordinal);
        Assert.Contains("Children =\n            {\n                image,\n                annotationLayer", code, StringComparison.Ordinal);
        Assert.Contains("UpdateAnnotationLayerInput", code, StringComparison.Ordinal);
    }

    private static XDocument LoadDesktopProject()
    {
        var projectPath = GetRepositoryPath("AnoPDF.Desktop", "AnoPDF.Desktop.csproj");

        return XDocument.Load(projectPath);
    }

    private static string GetRepositoryPath(params string[] paths)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", .. paths]));
    }

    private static string? GetProperty(XContainer project, string propertyName)
    {
        return project
            .Descendants(propertyName)
            .Select(element => element.Value)
            .FirstOrDefault();
    }

    private static double GetContrastRatio(string backgroundHex, string foregroundHex)
    {
        var background = GetRelativeLuminance(backgroundHex);
        var foreground = GetRelativeLuminance(foregroundHex);
        var lighter = Math.Max(background, foreground);
        var darker = Math.Min(background, foreground);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double GetRelativeLuminance(string hex)
    {
        var red = Convert.ToInt32(hex[1..3], 16) / 255.0;
        var green = Convert.ToInt32(hex[3..5], 16) / 255.0;
        var blue = Convert.ToInt32(hex[5..7], 16) / 255.0;

        return (0.2126 * ToLinear(red)) + (0.7152 * ToLinear(green)) + (0.0722 * ToLinear(blue));
    }

    private static double ToLinear(double channel)
    {
        return channel <= 0.03928
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
