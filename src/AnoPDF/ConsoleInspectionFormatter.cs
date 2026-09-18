using System.Globalization;
using System.Text;

namespace PdfInspector;

public static class ConsoleInspectionFormatter
{
    public static string Format(PdfInspectionResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture, $"File: {result.FileName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Path: {result.FilePath}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Size: {FormatFileSize(result.FileSizeBytes)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Pages: {result.PageCount}");
        builder.AppendLine("Metadata:");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Title: {result.Metadata.Title}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Author: {result.Metadata.Author}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Subject: {result.Metadata.Subject}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Keywords: {result.Metadata.Keywords}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Creator: {result.Metadata.Creator}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Producer: {result.Metadata.Producer}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Creation date: {result.Metadata.CreationDate}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"  Modified date: {result.Metadata.ModifiedDate}");
        builder.AppendLine("Pages:");

        foreach (var page in result.Pages)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"  Page {page.PageNumber}: {FormatNumber(page.Width)} x {FormatNumber(page.Height)}, Rotation: {page.RotationDegrees}, Text chars: {page.TextLength}");

            if (page.HasText)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Text sample: {page.TextSample}");
            }
            else
            {
                builder.AppendLine("    Text extraction: none");
            }
        }

        return builder.ToString();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{bytes} {units[unitIndex]}")
            : string.Create(CultureInfo.InvariantCulture, $"{size:0.##} {units[unitIndex]}");
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
