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
        var page = pageRenderer.RenderPage(inspection.FilePath, pageNumber: 1, renderSettings);

        CurrentState = new PdfViewerState(document, page);
        return CurrentState;
    }

    public PdfViewerState ShowPage(int pageNumber)
    {
        if (CurrentState is null)
        {
            throw new InvalidOperationException("A PDF must be open before navigating pages.");
        }

        if (pageNumber < 1 || pageNumber > CurrentState.Document.PageCount)
        {
            throw new PdfRenderException(
                PdfRenderFailure.PageOutOfRange,
                $"PDF page {pageNumber} is outside the document page range 1..{CurrentState.Document.PageCount}.");
        }

        var page = pageRenderer.RenderPage(CurrentState.Document.FilePath, pageNumber, renderSettings);
        CurrentState = CurrentState with { Page = page };
        return CurrentState;
    }

    public PdfViewerState RenderCurrentPage()
    {
        if (CurrentState is null)
        {
            throw new InvalidOperationException("A PDF must be open before rendering a page.");
        }

        return ShowPage(CurrentState.Page.PageNumber);
    }

    public PdfViewerState ChangeZoom(double scale)
    {
        if (CurrentState is null)
        {
            throw new InvalidOperationException("A PDF must be open before changing zoom.");
        }

        renderSettings = new PdfRenderSettings(scale, renderSettings.Dpi);
        return ShowPage(CurrentState.Page.PageNumber);
    }
}
