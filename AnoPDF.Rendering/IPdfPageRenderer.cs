namespace PdfInspector.Rendering;

public interface IPdfPageRenderer
{
    RenderedPdfPage RenderPage(
        string pdfPath,
        int pageNumber,
        PdfRenderSettings? settings = null);
}
