namespace PdfInspector.Rendering;

public enum PdfRenderFailure
{
    FileNotFound,
    NotPdfFile,
    PageOutOfRange,
    PdfiumUnavailable,
    RenderFailed
}

public sealed class PdfRenderException : Exception
{
    public PdfRenderException(PdfRenderFailure failure, string message)
        : base(message)
    {
        Failure = failure;
    }

    public PdfRenderException(PdfRenderFailure failure, string message, Exception innerException)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public PdfRenderFailure Failure { get; }
}
