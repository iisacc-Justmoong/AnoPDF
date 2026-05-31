using System.Globalization;
using System.Text;
using PdfInspector.Rendering;
using PdfInspector.Viewer;

namespace AnoPDF.Viewer.Tests;

public sealed class PdfViewerSessionTests
{
    [Fact]
    public void Open_loads_document_inspection_and_renders_first_page()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreateSinglePagePdf("Viewer"));
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);

        var state = session.Open(pdf.Path);

        Assert.Equal("viewer.pdf", state.Document.FileName);
        Assert.Equal(1, state.Document.PageCount);
        Assert.Equal(1, state.Page.PageNumber);
        Assert.Equal(pdf.Path, renderer.LastPath);
        Assert.Equal(1, renderer.LastPageNumber);
        Assert.Equal(state, session.CurrentState);
    }

    [Fact]
    public void RenderCurrentPage_renders_the_visible_page_again()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreateSinglePagePdf("Viewer"));
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);
        session.Open(pdf.Path);

        var state = session.RenderCurrentPage();

        Assert.Equal(2, renderer.RenderCallCount);
        Assert.Equal(1, state.Page.PageNumber);
        Assert.Equal(1, renderer.LastPageNumber);
    }

    [Fact]
    public void ChangeZoom_rerenders_current_page_with_requested_scale()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreateSinglePagePdf("Viewer"));
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);
        session.Open(pdf.Path);

        session.ChangeZoom(1.5);

        Assert.Equal(2, renderer.RenderCallCount);
        Assert.Equal(1.5, renderer.LastScale);
    }

    [Fact]
    public void ShowPage_rejects_navigation_before_a_document_is_open()
    {
        var session = new PdfViewerSession(new RecordingRenderer());

        var exception = Assert.Throws<InvalidOperationException>(() => session.ShowPage(1));

        Assert.Contains("open", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowPage_rejects_page_outside_open_document_range()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreateSinglePagePdf("Viewer"));
        var session = new PdfViewerSession(new RecordingRenderer());
        session.Open(pdf.Path);

        var exception = Assert.Throws<PdfRenderException>(() => session.ShowPage(2));

        Assert.Equal(PdfRenderFailure.PageOutOfRange, exception.Failure);
    }

    private sealed class RecordingRenderer : IPdfPageRenderer
    {
        public string? LastPath { get; private set; }

        public int? LastPageNumber { get; private set; }

        public double? LastScale { get; private set; }

        public int RenderCallCount { get; private set; }

        public RenderedPdfPage RenderPage(
            string pdfPath,
            int pageNumber,
            PdfRenderSettings? settings = null)
        {
            LastPath = pdfPath;
            LastPageNumber = pageNumber;
            LastScale = settings?.Scale;
            RenderCallCount++;

            return new RenderedPdfPage(
                pageNumber,
                pixelWidth: 2,
                pixelHeight: 2,
                stride: 8,
                dpi: settings?.Dpi ?? PdfRenderSettings.Default.Dpi,
                pixels: new byte[16]);
        }
    }
}

internal sealed class ViewerTestPdfFile : IDisposable
{
    private readonly string directoryPath;

    private ViewerTestPdfFile(string directoryPath, string path)
    {
        this.directoryPath = directoryPath;
        Path = path;
    }

    public string Path { get; }

    public static ViewerTestPdfFile Create(string fileName, byte[] bytes)
    {
        var directoryPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"anopdf-viewer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        var path = System.IO.Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(path, bytes);
        return new ViewerTestPdfFile(directoryPath, path);
    }

    public void Dispose()
    {
        Directory.Delete(directoryPath, recursive: true);
    }
}

internal static class ViewerTestPdfFactory
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
