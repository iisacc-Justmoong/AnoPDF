using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using PdfInspector;
using PdfInspector.Rendering;
using PdfInspector.Viewer;

namespace AnoPDF.Desktop;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType PdfFileType = new("PDF files")
    {
        Patterns = ["*.pdf"],
        MimeTypes = ["application/pdf"]
    };

    private readonly PdfViewerSession viewerSession = new(
        new PdfInspectionService(),
        new PdfiumPageRenderer(),
        PdfRenderSettings.Default);

    public MainWindow()
    {
        InitializeComponent();
        ShowStartView();
        UpdateControls();
    }

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "PDF 열기",
            AllowMultiple = false,
            FileTypeFilter = [PdfFileType, FilePickerFileTypes.All]
        });

        if (files.Count > 0)
        {
            OpenDocument(files[0].Path.LocalPath);
        }
    }

    private void PreviousPageButton_Click(object? sender, RoutedEventArgs e)
    {
        var state = viewerSession.CurrentState;
        if (state is null)
        {
            return;
        }

        ShowPage(state.Page.PageNumber - 1);
    }

    private void RenderPageButton_Click(object? sender, RoutedEventArgs e)
    {
        if (viewerSession.CurrentState is null)
        {
            return;
        }

        TryRender(viewerSession.RenderCurrentPage);
    }

    private void NextPageButton_Click(object? sender, RoutedEventArgs e)
    {
        var state = viewerSession.CurrentState;
        if (state is null)
        {
            return;
        }

        ShowPage(state.Page.PageNumber + 1);
    }

    private void ZoomSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded || viewerSession.CurrentState is null)
        {
            return;
        }

        TryRender(() => viewerSession.ChangeZoom(ZoomSlider.Value));
    }

    private void OpenDocument(string pdfPath)
    {
        SelectedPathText.Text = pdfPath;
        TryRender(() => viewerSession.Open(pdfPath, new PdfRenderSettings(ZoomSlider.Value)));
    }

    private void ShowPage(int pageNumber)
    {
        TryRender(() => viewerSession.ShowPage(pageNumber));
    }

    private void TryRender(Func<PdfViewerState> renderAction)
    {
        try
        {
            StatusText.Text = "렌더링 중";

            var state = renderAction();
            DisplayState(state);
            StatusText.Text = "준비됨";
        }
        catch (PdfInspectionException exception)
        {
            ShowFailure(exception.Message);
        }
        catch (PdfRenderException exception)
        {
            ShowFailure(exception.Message);
        }
        finally
        {
            UpdateControls();
        }
    }

    private void DisplayState(PdfViewerState state)
    {
        DocumentTitleText.Text = state.Document.FileName;
        PageNumberText.Text = $"{state.Page.PageNumber} / {state.Document.PageCount}";
        PageImage.Source = CreateBitmap(state.Page);
        ShowViewerView();
    }

    private void ShowFailure(string message)
    {
        if (viewerSession.CurrentState is null)
        {
            PageImage.Source = null;
            ShowStartView();
        }

        StatusText.Text = message;
    }

    private void UpdateControls()
    {
        var state = viewerSession.CurrentState;
        var hasDocument = state is not null;

        PreviousPageButton.IsEnabled = hasDocument && state!.Page.PageNumber > 1;
        RenderPageButton.IsEnabled = hasDocument;
        NextPageButton.IsEnabled = hasDocument && state!.Page.PageNumber < state.Document.PageCount;
        ZoomSlider.IsEnabled = hasDocument;

        if (!hasDocument)
        {
            PageNumberText.Text = "0 / 0";
        }
    }

    private void ShowStartView()
    {
        StartView.IsVisible = true;
        ViewerView.IsVisible = false;
    }

    private void ShowViewerView()
    {
        StartView.IsVisible = false;
        ViewerView.IsVisible = true;
    }

    private static Bitmap CreateBitmap(RenderedPdfPage page)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(page.PixelWidth, page.PixelHeight),
            new Avalonia.Vector(page.Dpi, page.Dpi),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        using var framebuffer = bitmap.Lock();
        Marshal.Copy(page.Pixels, 0, framebuffer.Address, page.Pixels.Length);
        return bitmap;
    }
}
