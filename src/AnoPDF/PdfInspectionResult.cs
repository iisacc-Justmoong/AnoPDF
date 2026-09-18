namespace PdfInspector;

public sealed record PdfInspectionResult(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    int PageCount,
    PdfMetadataInspectionResult Metadata,
    IReadOnlyList<PageInspectionResult> Pages);

public sealed record PdfMetadataInspectionResult(
    string Title,
    string Author,
    string Subject,
    string Keywords,
    string Creator,
    string Producer,
    string CreationDate,
    string ModifiedDate);

public sealed record PageInspectionResult(
    int PageNumber,
    double Width,
    double Height,
    int RotationDegrees,
    int TextLength,
    bool HasText,
    string TextSample);
