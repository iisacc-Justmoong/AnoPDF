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
    public void Desktop_window_has_a_bottom_drawing_toolbar_with_basic_tools()
    {
        var markup = File.ReadAllText(GetRepositoryPath("AnoPDF.Desktop", "MainWindow.axaml"));

        Assert.Contains("DrawingToolbar", markup, StringComparison.Ordinal);
        Assert.Contains("PanToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("PenToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("HighlighterToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("EraserToolButton", markup, StringComparison.Ordinal);
        Assert.Contains("DrawingColorBlackButton", markup, StringComparison.Ordinal);
        Assert.Contains("DrawingColorRedButton", markup, StringComparison.Ordinal);
        Assert.Contains("DrawingColorBlueButton", markup, StringComparison.Ordinal);
        Assert.Contains("StrokeWidthSlider", markup, StringComparison.Ordinal);
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
}
