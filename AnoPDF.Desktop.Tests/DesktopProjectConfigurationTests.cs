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
