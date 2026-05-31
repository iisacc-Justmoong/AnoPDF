using PdfInspector.Rendering;

namespace PdfInspector.Viewer;

public sealed record PdfViewerState(
    PdfViewerDocument Document,
    RenderedPdfPage Page);
