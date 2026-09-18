using Docnet.Core;
using Docnet.Core.Exceptions;
using Docnet.Core.Models;

namespace PdfInspector.Rendering;

public sealed class PdfiumPageRenderer : IPdfPageRenderer
{
    public RenderedPdfPage RenderPage(
        string pdfPath,
        int pageNumber,
        PdfRenderSettings? settings = null)
    {
        var fileInfo = ValidateInput(pdfPath, pageNumber);
        var renderSettings = settings ?? PdfRenderSettings.Default;

        try
        {
            using var document = DocLib.Instance.GetDocReader(
                fileInfo.FullName,
                new PageDimensions(renderSettings.Dpi / 72.0 * renderSettings.Scale));

            if (pageNumber > document.GetPageCount())
            {
                throw new PdfRenderException(
                    PdfRenderFailure.PageOutOfRange,
                    $"PDF page {pageNumber} is outside the document page range 1..{document.GetPageCount()}.");
            }

            using var page = document.GetPageReader(pageNumber - 1);
            var width = page.GetPageWidth();
            var height = page.GetPageHeight();
            var pixels = page.GetImage(RenderFlags.RenderAnnotations | RenderFlags.OptimizeTextForLcd);

            return new RenderedPdfPage(
                pageNumber,
                width,
                height,
                checked(width * 4),
                renderSettings.Dpi,
                pixels);
        }
        catch (PdfRenderException)
        {
            throw;
        }
        catch (DllNotFoundException exception)
        {
            throw CreateUnavailableException(exception);
        }
        catch (EntryPointNotFoundException exception)
        {
            throw CreateUnavailableException(exception);
        }
        catch (BadImageFormatException exception)
        {
            throw CreateUnavailableException(exception);
        }
        catch (DocnetLoadDocumentException exception)
        {
            throw new PdfRenderException(
                PdfRenderFailure.RenderFailed,
                $"PDFium failed to open the document for rendering: {fileInfo.FullName}",
                exception);
        }
        catch (DocnetException exception)
        {
            throw new PdfRenderException(
                PdfRenderFailure.RenderFailed,
                $"PDFium failed to render the page: {fileInfo.FullName}",
                exception);
        }
    }

    private static FileInfo ValidateInput(string pdfPath, int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw new PdfRenderException(
                PdfRenderFailure.PageOutOfRange,
                "Page number must be one-based.");
        }

        if (!string.Equals(Path.GetExtension(pdfPath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfRenderException(
                PdfRenderFailure.NotPdfFile,
                "The input file must have a .pdf extension.");
        }

        var fileInfo = new FileInfo(pdfPath);
        if (!fileInfo.Exists)
        {
            throw new PdfRenderException(
                PdfRenderFailure.FileNotFound,
                $"PDF file was not found: {pdfPath}");
        }

        return fileInfo;
    }

    private static PdfRenderException CreateUnavailableException(Exception exception)
    {
        return new PdfRenderException(
            PdfRenderFailure.PdfiumUnavailable,
            "PDFium native library is not available. Install or copy the matching PDFium native binaries.",
            exception);
    }
}
