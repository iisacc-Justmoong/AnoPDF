using PdfInspector.Rendering;

namespace PdfInspector.Viewer;

public sealed class PdfViewerSession
{
    private readonly PdfInspectionService inspectionService;
    private readonly IPdfPageRenderer pageRenderer;
    private PdfRenderSettings renderSettings;

    public PdfViewerSession(IPdfPageRenderer pageRenderer)
        : this(new PdfInspectionService(), pageRenderer, PdfRenderSettings.Default)
    {
    }

    public PdfViewerSession(
        PdfInspectionService inspectionService,
        IPdfPageRenderer pageRenderer,
        PdfRenderSettings renderSettings)
    {
        this.inspectionService = inspectionService;
        this.pageRenderer = pageRenderer;
        this.renderSettings = renderSettings;
    }

    public PdfViewerState? CurrentState { get; private set; }

    public PdfViewerState Open(string pdfPath, PdfRenderSettings? settings = null)
    {
        if (settings is not null)
        {
            renderSettings = settings;
        }

        var inspection = inspectionService.Inspect(pdfPath);
        var document = new PdfViewerDocument(
            inspection.FilePath,
            inspection.FileName,
            inspection.PageCount,
            inspection.Pages);
        var pages = RenderPages(document);

        CurrentState = new PdfViewerState(document, pages);
        return CurrentState;
    }

    public PdfViewerState RenderDocument()
    {
        if (CurrentState is null)
        {
            throw new InvalidOperationException("A PDF must be open before rendering pages.");
        }

        CurrentState = CurrentState with { Pages = RenderPages(CurrentState.Document) };
        return CurrentState;
    }

    public PdfViewerState ChangeZoom(double scale)
    {
        if (CurrentState is null)
        {
            throw new InvalidOperationException("A PDF must be open before changing zoom.");
        }

        renderSettings = new PdfRenderSettings(scale, renderSettings.Dpi);
        return RenderDocument();
    }

    private IReadOnlyList<RenderedPdfPage> RenderPages(PdfViewerDocument document)
    {
        var pages = new List<RenderedPdfPage>(document.PageCount);
        for (var pageNumber = 1; pageNumber <= document.PageCount; pageNumber++)
        {
            pages.Add(pageRenderer.RenderPage(document.FilePath, pageNumber, renderSettings));
        }

        return pages;
    }
}
