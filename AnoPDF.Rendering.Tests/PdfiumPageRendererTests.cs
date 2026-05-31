using PdfInspector.Rendering;

namespace AnoPDF.Rendering.Tests;

public sealed class PdfiumPageRendererTests
{
    [Fact]
    public void RenderPage_rejects_missing_pdf_before_loading_pdfium()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        var exception = Assert.Throws<PdfRenderException>(
            () => new PdfiumPageRenderer().RenderPage(missingPath, pageNumber: 1));

        Assert.Equal(PdfRenderFailure.FileNotFound, exception.Failure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RenderPage_rejects_invalid_page_number_before_loading_pdfium(int pageNumber)
    {
        var pdfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        File.WriteAllText(pdfPath, "%PDF-1.4");

        try
        {
            var exception = Assert.Throws<PdfRenderException>(
                () => new PdfiumPageRenderer().RenderPage(pdfPath, pageNumber));

            Assert.Equal(PdfRenderFailure.PageOutOfRange, exception.Failure);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }
}
