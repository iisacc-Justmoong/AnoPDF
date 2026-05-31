using System.Globalization;
using System.Text;
using PdfInspector;
using PdfInspector.Rendering;
using PdfInspector.Viewer;

namespace AnoPDF.Viewer.Tests;

public sealed class PdfViewerSessionTests
{
    [Fact]
    public void Open_loads_document_inspection_and_renders_every_page()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreatePdf("First", "Second", "Third"));
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);

        var state = session.Open(pdf.Path);

        Assert.Equal("viewer.pdf", state.Document.FileName);
        Assert.Equal(3, state.Document.PageCount);
        Assert.Equal([1, 2, 3], state.Pages.Select(page => page.PageNumber));
        Assert.Equal(pdf.Path, renderer.LastPath);
        Assert.Equal([1, 2, 3], renderer.PageNumbers);
        Assert.Equal(state, session.CurrentState);
    }

    [Fact]
    public void Open_rejects_missing_file_without_creating_viewer_state()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);

        var exception = Assert.Throws<PdfInspectionException>(() => session.Open(missingPath));

        Assert.Equal(PdfInspectionFailure.FileNotFound, exception.Failure);
        Assert.Null(session.CurrentState);
        Assert.Equal(0, renderer.RenderCallCount);
    }

    [Fact]
    public void RenderDocument_renders_every_page_again()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreatePdf("First", "Second"));
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);
        session.Open(pdf.Path);

        var state = session.RenderDocument();

        Assert.Equal(4, renderer.RenderCallCount);
        Assert.Equal([1, 2], state.Pages.Select(page => page.PageNumber));
        Assert.Equal([1, 2, 1, 2], renderer.PageNumbers);
    }

    [Fact]
    public void ChangeZoom_rerenders_every_page_with_requested_scale()
    {
        using var pdf = ViewerTestPdfFile.Create("viewer.pdf", ViewerTestPdfFactory.CreatePdf("First", "Second"));
        var renderer = new RecordingRenderer();
        var session = new PdfViewerSession(renderer);
        session.Open(pdf.Path);

        session.ChangeZoom(1.5);

        Assert.Equal(4, renderer.RenderCallCount);
        Assert.Equal([1, 2, 1, 2], renderer.PageNumbers);
        Assert.All(renderer.Scales.Skip(2), scale => Assert.Equal(1.5, scale));
    }

    [Fact]
    public void RenderDocument_rejects_rendering_before_a_document_is_open()
    {
        var session = new PdfViewerSession(new RecordingRenderer());

        var exception = Assert.Throws<InvalidOperationException>(() => session.RenderDocument());

        Assert.Contains("open", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangeZoom_rejects_zoom_before_a_document_is_open()
    {
        var session = new PdfViewerSession(new RecordingRenderer());

        var exception = Assert.Throws<InvalidOperationException>(() => session.ChangeZoom(1.25));

        Assert.Contains("open", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingRenderer : IPdfPageRenderer
    {
        public string? LastPath { get; private set; }

        public List<int> PageNumbers { get; } = [];

        public List<double?> Scales { get; } = [];

        public int RenderCallCount { get; private set; }

        public RenderedPdfPage RenderPage(
            string pdfPath,
            int pageNumber,
            PdfRenderSettings? settings = null)
        {
            LastPath = pdfPath;
            PageNumbers.Add(pageNumber);
            Scales.Add(settings?.Scale);
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
        return CreatePdf(text);
    }

    public static byte[] CreatePdf(params string[] pageTexts)
    {
        if (pageTexts.Length == 0)
        {
            throw new ArgumentException("At least one page is required.", nameof(pageTexts));
        }

        var pageIds = Enumerable
            .Range(0, pageTexts.Length)
            .Select(index => 4 + (index * 2))
            .ToArray();
        var kids = string.Join(" ", pageIds.Select(pageId => $"{pageId} 0 R"));
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{kids}] /Count {pageTexts.Length} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        for (var index = 0; index < pageTexts.Length; index++)
        {
            var contentId = pageIds[index] + 1;
            var content = $"BT\n/F1 12 Tf\n72 720 Td\n({EscapePdfString(pageTexts[index])}) Tj\nET";

            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Rotate 0 /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

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
