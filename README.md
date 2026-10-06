# AnoPDF

AnoPDF is a step-by-step foundation for the editor that draws on top of PDF, first reading, explaining, and then rendering the PDF. The current configuration consists of a console-based `PDF Inspector 0.1` and an Avalonia-based GUI Viewer.

## PDF Inspector 0.1

- It accepts the PDF file path as an execution argument.
- It checks for file existence and the `.pdf` extension.
- It opens the document with PdfPig to read the number of pages.
- It displays metadata such as title, author, creation tool, creation date, and modification date.
- It displays page number, page size, rotation value, text length, and the first 200 characters of text sample.
- Pages without text are marked as `Text extraction: none`.
- Saves the analysis results by default to `<filename>.analysis.json`.

## GUI Viewer

- `AnoPDF.Rendering` renders PDF pages to BGRA pixel buffers using the PDFium native package in Docnet.Core.
- `AnoPDF.Viewer` provides a session model that separates file opening, document inspection, full page rendering, and zoom level changes from the UI.
- `AnoPDF.Viewer` provides a PDF document model and a separate `PdfAnnotationLayer` ink stroke model.
- `AnoPDF.Desktop` is an Avalonia app that receives the PDF path from the file dialog in the initial view and switches to the PDF view only if the file is opened correctly.
- The subsequent view lists all rendered pages in the downward direction and allows users to freely navigate the entire document via scroll.
- The GUI clarifies the visual hierarchy between buttons and the document area by using a light work background, dark top/bottom toolbars, and high-contrast primary buttons.
- A default drawing set toolbar is placed at the bottom of the window. The current Pen can be used by mouse drag, and the HSL internal triangle color wheel, similar to the Photoshop color wheel mode, and stroke width slider values are used for the color and size of the new stroke. Highlighter and Eraser are still in preparation.
- Each rendered PDF page displays the same-sized `PdfAnnotationLayerCanvas` 1 layer overlaid on top of the PDF image. This layer is kept as a separate hierarchy from the PDF pixel buffer and handles only stroke input.

The minimum features of the current GUI Viewer are as follows.

- Open File: Selects the PDF file in the initial view and reads the document information.
- Direct File Dialog: Opens the file selection dialog directly with the `Open` button below the selected path in the initial view.
- Render Document: Re-render the entire document page using PDFium.
- Zoom: Change the zoom level and re-render the entire document page.
- Document Scroll: Move by scrolling with the entire page stacked vertically.
- Drawing Toolbar: Select the Pan/Pen tool, HSL internal color wheel, and line thickness from the bottom of the window.
- Pen Tool: After selecting Pen, drag with the left mouse button on PDF to draw a stroke on a separate annotation layer.

The file selector opens from `TopLevel.StorageProvider`, which is connected to the Avalonia window, and displays an error in the status bar in execution environments that do not support file opening.

`Microsoft.WindowsDesktop.App` is a dedicated runtime for Windows and cannot be run from macOS. Therefore, the GUI project is composed as a `net9.0` Avalonia app to enable desktop execution even on macOS arm64.

<a id="제외-범위"></a>

## Excluded Scope

- Do not perform text editing.
- Do not create features for saving annotations, drag and drop, and drawing shapes.

<a id="콘솔-실행"></a>

## Run Console

```bash
dotnet run --project src/AnoPDF/AnoPDF.csproj -- sample.pdf
```

You can specify the JSON output path.

```bash
dotnet run --project src/AnoPDF/AnoPDF.csproj -- sample.pdf --json sample.analysis.json
```

JSON  Saving can be omitted.

```bash
dotnet run --project src/AnoPDF/AnoPDF.csproj -- sample.pdf --no-json
```

<a id="gui-실행"></a>

## GUI  Run

Run the next command with  GUI  Viewer.

```bash
dotnet run --project src/AnoPDF.Desktop/AnoPDF.Desktop.csproj
```

<a id="검증"></a>

## Verification

The test creates a temporary minimum of  PDF  files to verify input validation, metadata, page size, text sample limits, pages without text,  JSON  saving, rendering geometry, PDFium rendering, file open session, full page rendering, comment layer stroke model, desktop project runtime configuration, file selector connection method, scrollable document view configuration, bottom default drawing toolbar, high contrast  GUI  chrome,  PDF  top overlay layer configuration.

```bash
dotnet test AnoPDF.sln
dotnet build src/AnoPDF.Desktop/AnoPDF.Desktop.csproj -c Release -o build
dotnet build/PdfInspector.dll sample.pdf
```

Currently,  PdfPig   NuGet  package has no stable version, so  `UglyToad.PdfPig`   `1.7.0-custom-5`  prerelease package is used.  PDF  Rendering uses  `Docnet.Core`   `2.6.0`  including PDFium native. GUI uses  `Avalonia`   `12.0.4`  and  `Avalonia.Desktop`   `12.0.4` . Pen input and comment layer are implemented using Avalonia's pointer events and custom control rendering, without adding separate external dependencies.

## Source layout

Application and library projects live under `src/`; automated test projects live under `tests/`. Build configuration stays at the root, and all build output belongs under `build/`.

Desktop configuration tests locate `AnoPDF.sln` from the test output directory, so repository-relative source checks also work with project-specific output folders under `build/`.
