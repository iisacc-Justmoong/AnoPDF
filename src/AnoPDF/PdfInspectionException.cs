namespace PdfInspector;

public enum PdfInspectionFailure
{
    EmptyPath,
    FileNotFound,
    NotPdfFile,
    FileCannotBeOpened,
    EncryptedPdf,
    InvalidPdf
}

public sealed class PdfInspectionException : Exception
{
    public PdfInspectionException(PdfInspectionFailure failure, string message)
        : base(message)
    {
        Failure = failure;
    }

    public PdfInspectionException(PdfInspectionFailure failure, string message, Exception innerException)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public PdfInspectionFailure Failure { get; }
}
