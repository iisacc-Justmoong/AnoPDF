using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Exceptions;

namespace PdfInspector;

public sealed partial class PdfInspectionService
{
    public PdfInspectionResult Inspect(string pdfPath)
    {
        var fileInfo = ValidateInput(pdfPath);

        try
        {
            using var document = PdfDocument.Open(fileInfo.FullName);
            var pages = document.GetPages()
                .Select(InspectPage)
                .ToArray();

            return new PdfInspectionResult(
                fileInfo.FullName,
                fileInfo.Name,
                fileInfo.Length,
                document.NumberOfPages,
                InspectMetadata(document.Information),
                pages);
        }
        catch (PdfDocumentEncryptedException exception)
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.EncryptedPdf,
                $"PDF is encrypted and cannot be inspected without a password: {fileInfo.FullName}",
                exception);
        }
        catch (PdfDocumentFormatException exception)
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.InvalidPdf,
                $"PDF appears to be damaged or invalid: {fileInfo.FullName}",
                exception);
        }
        catch (IOException exception)
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.FileCannotBeOpened,
                $"PDF file cannot be opened: {fileInfo.FullName}",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.FileCannotBeOpened,
                $"PDF file cannot be opened because access was denied: {fileInfo.FullName}",
                exception);
        }
    }

    private static FileInfo ValidateInput(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.EmptyPath,
                "A PDF file path is required.");
        }

        if (!string.Equals(Path.GetExtension(pdfPath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.NotPdfFile,
                "The input file must have a .pdf extension.");
        }

        var fileInfo = new FileInfo(pdfPath);
        if (!fileInfo.Exists)
        {
            throw new PdfInspectionException(
                PdfInspectionFailure.FileNotFound,
                $"PDF file was not found: {pdfPath}");
        }

        return fileInfo;
    }

    private static PdfMetadataInspectionResult InspectMetadata(DocumentInformation information)
    {
        return new PdfMetadataInspectionResult(
            ValueOrFallback(information.Title),
            ValueOrFallback(information.Author),
            ValueOrFallback(information.Subject),
            ValueOrFallback(information.Keywords),
            ValueOrFallback(information.Creator),
            ValueOrFallback(information.Producer),
            ValueOrFallback(information.CreationDate),
            ValueOrFallback(information.ModifiedDate));
    }

    private static PageInspectionResult InspectPage(Page page)
    {
        var text = NormalizeExtractedText(page.Text);

        return new PageInspectionResult(
            page.Number,
            page.Width,
            page.Height,
            page.Rotation.Value,
            text.Length,
            text.Length > 0,
            text.Length <= PdfInspectionDefaults.TextSampleLength
                ? text
                : text[..PdfInspectionDefaults.TextSampleLength]);
    }

    private static string ValueOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? PdfInspectionDefaults.NotProvided
            : value.Trim();
    }

    private static string NormalizeExtractedText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return WhitespacePattern().Replace(text, " ").Trim();
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();
}
