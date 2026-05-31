using System.Globalization;
using System.Text;
using System.Text.Json;
using PdfInspector;

namespace AnoPDF.Tests;

public sealed class PdfInspectionServiceTests
{
    [Fact]
    public void Inspect_reads_document_facts_metadata_and_page_summary()
    {
        using var pdf = TestPdfFile.Create(
            "sample.pdf",
            TestPdfFactory.CreateSinglePagePdf(
                "Hello PDF Inspector",
                width: 612,
                height: 792,
                metadata: new TestPdfMetadata(
                    Title: "Fixture Title",
                    Author: "Fixture Author",
                    Creator: "AnoPDF Tests",
                    Producer: "Handcrafted PDF")));

        var result = new PdfInspectionService().Inspect(pdf.Path);

        Assert.Equal(pdf.Path, result.FilePath);
        Assert.Equal("sample.pdf", result.FileName);
        Assert.Equal(new FileInfo(pdf.Path).Length, result.FileSizeBytes);
        Assert.Equal(1, result.PageCount);
        Assert.Equal("Fixture Title", result.Metadata.Title);
        Assert.Equal("Fixture Author", result.Metadata.Author);
        Assert.Equal("AnoPDF Tests", result.Metadata.Creator);
        Assert.Equal("Handcrafted PDF", result.Metadata.Producer);

        var page = Assert.Single(result.Pages);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(612, page.Width);
        Assert.Equal(792, page.Height);
        Assert.Equal(0, page.RotationDegrees);
        Assert.True(page.HasText);
        Assert.Contains("Hello PDF Inspector", page.TextSample);
        Assert.True(page.TextLength >= "Hello PDF Inspector".Length);
    }

    [Fact]
    public void Inspect_marks_missing_metadata_and_textless_pages_without_failing()
    {
        using var pdf = TestPdfFile.Create(
            "scan.pdf",
            TestPdfFactory.CreateSinglePagePdf(text: string.Empty, width: 595, height: 842));

        var result = new PdfInspectionService().Inspect(pdf.Path);

        Assert.Equal(PdfInspectionDefaults.NotProvided, result.Metadata.Title);
        Assert.Equal(PdfInspectionDefaults.NotProvided, result.Metadata.Author);

        var page = Assert.Single(result.Pages);
        Assert.False(page.HasText);
        Assert.Equal(0, page.TextLength);
        Assert.Equal(string.Empty, page.TextSample);

        var consoleText = ConsoleInspectionFormatter.Format(result);
        Assert.Contains("Text extraction: none", consoleText);
    }

    [Fact]
    public void Inspect_limits_each_page_text_sample_to_two_hundred_characters()
    {
        var longText = new string('A', 260);
        using var pdf = TestPdfFile.Create(
            "long-text.pdf",
            TestPdfFactory.CreateSinglePagePdf(longText, width: 612, height: 792));

        var result = new PdfInspectionService().Inspect(pdf.Path);

        var page = Assert.Single(result.Pages);
        Assert.Equal(200, page.TextSample.Length);
        Assert.All(page.TextSample, character => Assert.Equal('A', character));
    }

    [Fact]
    public void Inspect_rejects_missing_files_before_opening_pdf_library()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        var exception = Assert.Throws<PdfInspectionException>(
            () => new PdfInspectionService().Inspect(missingPath));

        Assert.Equal(PdfInspectionFailure.FileNotFound, exception.Failure);
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inspect_rejects_non_pdf_extension()
    {
        var textPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(textPath, "not a pdf");

        try
        {
            var exception = Assert.Throws<PdfInspectionException>(
                () => new PdfInspectionService().Inspect(textPath));

            Assert.Equal(PdfInspectionFailure.NotPdfFile, exception.Failure);
            Assert.Contains(".pdf", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(textPath);
        }
    }

    [Fact]
    public void Inspect_reports_invalid_pdf_when_pdf_extension_file_is_damaged()
    {
        var damagedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        File.WriteAllText(damagedPath, "not a real pdf");

        try
        {
            var exception = Assert.Throws<PdfInspectionException>(
                () => new PdfInspectionService().Inspect(damagedPath));

            Assert.Equal(PdfInspectionFailure.InvalidPdf, exception.Failure);
            Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(damagedPath);
        }
    }

    [Fact]
    public void Save_writes_default_analysis_json_next_to_pdf()
    {
        using var pdf = TestPdfFile.Create(
            "sample.pdf",
            TestPdfFactory.CreateSinglePagePdf("JSON export", width: 612, height: 792));
        var result = new PdfInspectionService().Inspect(pdf.Path);
        var outputPath = PdfInspectionJsonWriter.GetDefaultOutputPath(pdf.Path);

        PdfInspectionJsonWriter.Save(result, outputPath);

        Assert.True(File.Exists(outputPath));
        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        Assert.Equal("sample.pdf", document.RootElement.GetProperty("fileName").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("pageCount").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("pages").GetArrayLength());
    }
}

internal sealed class TestPdfFile : IDisposable
{
    private readonly string directoryPath;

    private TestPdfFile(string directoryPath, string path)
    {
        this.directoryPath = directoryPath;
        Path = path;
    }

    public string Path { get; }

    public static TestPdfFile Create(string fileName, byte[] bytes)
    {
        var directoryPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"anopdf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        var path = System.IO.Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(path, bytes);
        return new TestPdfFile(directoryPath, path);
    }

    public void Dispose()
    {
        Directory.Delete(directoryPath, recursive: true);
    }
}

internal sealed record TestPdfMetadata(
    string Title,
    string Author,
    string Creator,
    string Producer);

internal static class TestPdfFactory
{
    public static byte[] CreateSinglePagePdf(
        string text,
        double width,
        double height,
        TestPdfMetadata? metadata = null)
    {
        var content = string.IsNullOrEmpty(text)
            ? string.Empty
            : $"BT\n/F1 12 Tf\n72 {height - 72:0.###} Td\n({EscapePdfString(text)}) Tj\nET";

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            string.Create(
                CultureInfo.InvariantCulture,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width:0.###} {height:0.###}] /Rotate 0 /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"
        };

        if (metadata is not null)
        {
            objects.Add(
                $"<< /Title ({EscapePdfString(metadata.Title)}) /Author ({EscapePdfString(metadata.Author)}) /Creator ({EscapePdfString(metadata.Creator)}) /Producer ({EscapePdfString(metadata.Producer)}) >>");
        }

        return WritePdf(objects, includeInfoObject: metadata is not null);
    }

    private static byte[] WritePdf(IReadOnlyList<string> objects, bool includeInfoObject)
    {
        var builder = new StringBuilder();
        var offsets = new List<int> { 0 };

        builder.Append("%PDF-1.4\n");
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Count + 1}\n");
        builder.Append("0000000000 65535 f \n");

        for (var i = 1; i <= objects.Count; i++)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{offsets[i]:D10} 00000 n \n");
        }

        builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R");
        if (includeInfoObject)
        {
            builder.Append(CultureInfo.InvariantCulture, $" /Info {objects.Count} 0 R");
        }

        builder.Append(CultureInfo.InvariantCulture, $" >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string EscapePdfString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
