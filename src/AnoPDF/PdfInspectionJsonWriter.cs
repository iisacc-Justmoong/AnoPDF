using System.Text.Json;

namespace PdfInspector;

public static class PdfInspectionJsonWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string GetDefaultOutputPath(string pdfPath)
    {
        var directoryPath = Path.GetDirectoryName(Path.GetFullPath(pdfPath)) ?? Directory.GetCurrentDirectory();
        var fileName = $"{Path.GetFileNameWithoutExtension(pdfPath)}.analysis.json";
        return Path.Combine(directoryPath, fileName);
    }

    public static void Save(PdfInspectionResult result, string outputPath)
    {
        var directoryPath = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(result, SerializerOptions);
        File.WriteAllText(outputPath, json);
    }
}
