using PdfInspector.Rendering;
using System.Globalization;
using System.Text;

namespace AnoPDF.Rendering.Tests;

public sealed class PdfiumPageRendererTests
{
    [Fact]
    public void RenderPage_renders_pdf_page_with_bgra_pixels()
    {
        using var pdf = RenderTestPdfFile.Create("render.pdf", RenderTestPdfFactory.CreateSinglePagePdf("Render"));

        var page = new PdfiumPageRenderer().RenderPage(pdf.Path, pageNumber: 1);

        Assert.Equal(1, page.PageNumber);
        Assert.True(page.PixelWidth > 0);
        Assert.True(page.PixelHeight > 0);
        Assert.Equal(page.PixelWidth * 4, page.Stride);
        Assert.Equal(page.Stride * page.PixelHeight, page.Pixels.Length);
    }

    [Fact]
    public void RenderPage_rejects_missing_pdf_before_loading_pdfium()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        var exception = Assert.Throws<PdfRenderException>(
            () => new PdfiumPageRenderer().RenderPage(missingPath, pageNumber: 1));

        Assert.Equal(PdfRenderFailure.FileNotFound, exception.Failure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RenderPage_rejects_invalid_page_number_before_loading_pdfium(int pageNumber)
    {
        var pdfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        File.WriteAllText(pdfPath, "%PDF-1.4");

        try
        {
            var exception = Assert.Throws<PdfRenderException>(
                () => new PdfiumPageRenderer().RenderPage(pdfPath, pageNumber));

            Assert.Equal(PdfRenderFailure.PageOutOfRange, exception.Failure);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }
}

internal sealed class RenderTestPdfFile : IDisposable
{
    private readonly string directoryPath;

    private RenderTestPdfFile(string directoryPath, string path)
    {
        this.directoryPath = directoryPath;
        Path = path;
    }

    public string Path { get; }

    public static RenderTestPdfFile Create(string fileName, byte[] bytes)
    {
        var directoryPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"anopdf-render-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        var path = System.IO.Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(path, bytes);
        return new RenderTestPdfFile(directoryPath, path);
    }

    public void Dispose()
    {
        Directory.Delete(directoryPath, recursive: true);
    }
}

internal static class RenderTestPdfFactory
{
    public static byte[] CreateSinglePagePdf(string text)
    {
        var content = $"BT\n/F1 12 Tf\n72 720 Td\n({EscapePdfString(text)}) Tj\nET";
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Rotate 0 /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"
        };

        return WritePdf(objects);
    }

    private static byte[] WritePdf(IReadOnlyList<string> objects)
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

        builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
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
