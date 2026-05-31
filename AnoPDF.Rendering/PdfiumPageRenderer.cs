using System.Runtime.InteropServices;
using PDFiumSharp;
using PDFiumSharp.Enums;
using PDFiumSharp.Types;

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
            EnsurePdfiumIsAvailable();

            using var document = new PdfDocument(fileInfo.FullName);
            if (pageNumber > document.Pages.Count)
            {
                throw new PdfRenderException(
                    PdfRenderFailure.PageOutOfRange,
                    $"PDF page {pageNumber} is outside the document page range 1..{document.Pages.Count}.");
            }

            using var page = document.Pages[pageNumber - 1];
            var pixelSize = PdfRenderGeometry.CalculatePixelSize(page.Width, page.Height, renderSettings);
            using var bitmap = new PDFiumBitmap(pixelSize.Width, pixelSize.Height, hasAlpha: true);

            bitmap.Fill(new FPDF_COLOR(r: 255, g: 255, b: 255, a: 255));
            page.Render(
                bitmap,
                (0, 0, pixelSize.Width, pixelSize.Height),
                PageOrientations.Normal,
                RenderingFlags.Annotations | RenderingFlags.LcdText);

            var pixels = new byte[checked(bitmap.Stride * bitmap.Height)];
            Marshal.Copy(bitmap.Scan0, pixels, 0, pixels.Length);

            return new RenderedPdfPage(
                pageNumber,
                bitmap.Width,
                bitmap.Height,
                bitmap.Stride,
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
        catch (PDFiumException exception)
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

    private static void EnsurePdfiumIsAvailable()
    {
        if (PDFium.IsAvailable)
        {
            return;
        }

        throw new PdfRenderException(
            PdfRenderFailure.PdfiumUnavailable,
            "PDFium native library is not available. Install or copy the matching PDFium native binaries.");
    }

    private static PdfRenderException CreateUnavailableException(Exception exception)
    {
        return new PdfRenderException(
            PdfRenderFailure.PdfiumUnavailable,
            "PDFium native library is not available. Install or copy the matching PDFium native binaries.",
            exception);
    }
}
