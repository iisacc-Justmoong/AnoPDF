using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PdfInspector;
using PdfInspector.Rendering;
using PdfInspector.Viewer;

namespace AnoPDF.Desktop;

public partial class MainWindow : Window
{
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

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            Title = "PDF 열기"
        };

        if (dialog.ShowDialog(this) == true)
        {
            OpenDocument(dialog.FileName);
        }
    }

    private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        var state = viewerSession.CurrentState;
        if (state is null)
        {
            return;
        }

        ShowPage(state.Page.PageNumber - 1);
    }

    private void RenderPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (viewerSession.CurrentState is null)
        {
            return;
        }

        TryRender(viewerSession.RenderCurrentPage);
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        var state = viewerSession.CurrentState;
        if (state is null)
        {
            return;
        }

        ShowPage(state.Page.PageNumber + 1);
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
            Mouse.OverrideCursor = Cursors.Wait;
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
            Mouse.OverrideCursor = null;
            UpdateControls();
        }
    }

    private void DisplayState(PdfViewerState state)
    {
        DocumentTitleText.Text = state.Document.FileName;
        PageNumberText.Text = $"{state.Page.PageNumber} / {state.Document.PageCount}";
        PageImage.Source = CreateBitmapSource(state.Page);
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
        MessageBox.Show(this, message, "AnoPDF", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        StartView.Visibility = Visibility.Visible;
        ViewerView.Visibility = Visibility.Collapsed;
    }

    private void ShowViewerView()
    {
        StartView.Visibility = Visibility.Collapsed;
        ViewerView.Visibility = Visibility.Visible;
    }

    private static BitmapSource CreateBitmapSource(RenderedPdfPage page)
    {
        var source = BitmapSource.Create(
            page.PixelWidth,
            page.PixelHeight,
            page.Dpi,
            page.Dpi,
            PixelFormats.Bgra32,
            palette: null,
            page.Pixels,
            page.Stride);

        source.Freeze();
        return source;
    }
}
