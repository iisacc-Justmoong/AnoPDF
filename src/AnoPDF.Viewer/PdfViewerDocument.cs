using PdfInspector;

namespace PdfInspector.Viewer;

public sealed record PdfViewerDocument(
    string FilePath,
    string FileName,
    int PageCount,
    IReadOnlyList<PageInspectionResult> Pages);
