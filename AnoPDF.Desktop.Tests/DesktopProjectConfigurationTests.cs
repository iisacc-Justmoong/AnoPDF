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

    private static XDocument LoadDesktopProject()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AnoPDF.Desktop",
            "AnoPDF.Desktop.csproj"));

        return XDocument.Load(projectPath);
    }

    private static string? GetProperty(XContainer project, string propertyName)
    {
        return project
            .Descendants(propertyName)
            .Select(element => element.Value)
            .FirstOrDefault();
    }
}
