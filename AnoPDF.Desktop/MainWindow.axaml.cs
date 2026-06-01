using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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
    private readonly List<PdfAnnotationLayerCanvas> annotationLayers = [];
    private DrawingTool selectedDrawingTool = DrawingTool.Pan;
    private Color selectedPenColor = Color.Parse("#2563EB");

    public MainWindow()
    {
        InitializeComponent();
        selectedPenColor = HslRgbTriangleColorPicker.SelectedColor;
        HslRgbTriangleColorPicker.SelectedColorChanged += HslRgbTriangleColorPicker_SelectedColorChanged;
        ShowStartView();
        UpdateControls();
    }

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        await OpenPdfFromDialogAsync();
    }

    private async Task OpenPdfFromDialogAsync()
    {
        SetOpenButtonsEnabled(false);

        try
        {
            StatusText.Text = "Choosing file";

            var pdfPath = await PickPdfPathAsync();
            if (pdfPath is null)
            {
                StatusText.Text = "Ready";
                return;
            }

            OpenDocument(pdfPath);
        }
        catch (Exception exception)
        {
            ShowFailure($"Could not open the file dialog: {exception.Message}");
        }
        finally
        {
            SetOpenButtonsEnabled(true);
            UpdateControls();
        }
    }

    private async Task<string?> PickPdfPathAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            throw new InvalidOperationException("The current window is not attached to the file dialog.");
        }

        var storageProvider = topLevel.StorageProvider;
        if (!storageProvider.CanOpen)
        {
            throw new InvalidOperationException("The current runtime does not support opening files.");
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF",
            AllowMultiple = false,
            FileTypeFilter = [PdfFileType, FilePickerFileTypes.All]
        });

        if (files.Count == 0)
        {
            return null;
        }

        var localPath = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new InvalidOperationException("The selected PDF does not expose a local file path.");
        }

        return localPath;
    }

    private void RenderDocumentButton_Click(object? sender, RoutedEventArgs e)
    {
        if (viewerSession.CurrentState is null)
        {
            return;
        }

        TryRender(viewerSession.RenderDocument);
    }

    private void PanToolButton_Click(object? sender, RoutedEventArgs e)
    {
        SelectDrawingTool(DrawingTool.Pan);
    }

    private void PenToolButton_Click(object? sender, RoutedEventArgs e)
    {
        SelectDrawingTool(DrawingTool.Pen);
    }

    private void ZoomSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!IsLoaded || viewerSession.CurrentState is null)
        {
            return;
        }

        TryRender(() => viewerSession.ChangeZoom(ZoomSlider.Value));
    }

    private void StrokeWidthSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdateAnnotationLayerInput();
    }

    private void HslRgbTriangleColorPicker_SelectedColorChanged(object? sender, Color color)
    {
        selectedPenColor = color;
        UpdateAnnotationLayerInput();
    }

    private void SelectDrawingTool(DrawingTool drawingTool)
    {
        selectedDrawingTool = drawingTool;
        StatusText.Text = drawingTool == DrawingTool.Pen ? "Pen tool" : "Ready";
        UpdateControls();
    }

    private void OpenDocument(string pdfPath)
    {
        SelectedPathText.Text = pdfPath;
        TryRender(() => viewerSession.Open(pdfPath, new PdfRenderSettings(ZoomSlider.Value)));
    }

    private void TryRender(Func<PdfViewerState> renderAction)
    {
        try
        {
            StatusText.Text = "Rendering";

            var state = renderAction();
            DisplayState(state);
            StatusText.Text = "Ready";
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
        PageCountText.Text = $"{state.Document.PageCount} pages";
        DisplayPages(state.Pages);
        ShowViewerView();
    }

    private void ShowFailure(string message)
    {
        if (viewerSession.CurrentState is null)
        {
            PageStack.Children.Clear();
            ShowStartView();
        }

        StatusText.Text = message;
    }

    private void UpdateControls()
    {
        var state = viewerSession.CurrentState;
        var hasDocument = state is not null;

        RenderDocumentButton.IsEnabled = hasDocument;
        ZoomSlider.IsEnabled = hasDocument;
        PanToolButton.IsEnabled = hasDocument;
        PenToolButton.IsEnabled = hasDocument;
        HighlighterToolButton.IsEnabled = false;
        EraserToolButton.IsEnabled = false;

        if (!hasDocument)
        {
            PageCountText.Text = "0 pages";
        }

        UpdateDrawingToolButtons();
        UpdateAnnotationLayerInput();
    }

    private void SetOpenButtonsEnabled(bool isEnabled)
    {
        DirectFileDialogButton.IsEnabled = isEnabled;
        OpenButton.IsEnabled = isEnabled;
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

    private void DisplayPages(IReadOnlyList<RenderedPdfPage> pages)
    {
        PageStack.Children.Clear();
        annotationLayers.Clear();

        foreach (var page in pages)
        {
            PageStack.Children.Add(CreatePageView(page));
        }
    }

    private Control CreatePageView(RenderedPdfPage page)
    {
        var label = new TextBlock
        {
            Text = $"Page {page.PageNumber}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.DimGray
        };

        var image = new Image
        {
            Source = CreateBitmap(page),
            Stretch = Stretch.None
        };

        var annotationLayer = new PdfAnnotationLayerCanvas(new PdfAnnotationLayer())
        {
            Width = page.PixelWidth,
            Height = page.PixelHeight,
            IsDrawingEnabled = selectedDrawingTool == DrawingTool.Pen,
            StrokeStyle = CreateCurrentPenStrokeStyle()
        };
        annotationLayers.Add(annotationLayer);

        var pageSurface = new Grid
        {
            Width = page.PixelWidth,
            Height = page.PixelHeight,
            ClipToBounds = true,
            Children =
            {
                image,
                annotationLayer
            }
        };

        var frame = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#D7DCE2")),
            BorderThickness = new Thickness(1),
            Child = pageSurface
        };

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
            Children =
            {
                label,
                frame
            }
        };
    }

    private void UpdateDrawingToolButtons()
    {
        PanToolButton.FontWeight = selectedDrawingTool == DrawingTool.Pan ? FontWeight.SemiBold : FontWeight.Normal;
        PenToolButton.FontWeight = selectedDrawingTool == DrawingTool.Pen ? FontWeight.SemiBold : FontWeight.Normal;
    }

    private void UpdateAnnotationLayerInput()
    {
        foreach (var annotationLayer in annotationLayers)
        {
            annotationLayer.IsDrawingEnabled = selectedDrawingTool == DrawingTool.Pen;
            annotationLayer.StrokeStyle = CreateCurrentPenStrokeStyle();
        }
    }

    private PenStrokeStyle CreateCurrentPenStrokeStyle()
    {
        return new PenStrokeStyle(StrokeWidthSlider.Value, ToInkColor(selectedPenColor));
    }

    private static InkColor ToInkColor(Color color)
    {
        return new InkColor(color.A, color.R, color.G, color.B);
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

    private enum DrawingTool
    {
        Pan,
        Pen
    }
}
