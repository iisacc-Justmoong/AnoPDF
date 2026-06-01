# AnoPDF

AnoPDF는 PDF 위에 그리는 편집기로 가기 전, PDF를 먼저 읽고 설명하고 렌더링하는 기반을 단계적으로 만든다. 현재 구성은 콘솔 기반 `PDF Inspector 0.1`과 Avalonia 기반 GUI Viewer이다.

## PDF Inspector 0.1

- PDF 파일 경로를 실행 인자로 받는다.
- 파일 존재 여부와 `.pdf` 확장자를 검사한다.
- PdfPig로 문서를 열어 페이지 수를 읽는다.
- 제목, 작성자, 생성 도구, 생성일, 수정일 등 메타데이터를 표시한다.
- 페이지 번호, 페이지 크기, 회전값, 텍스트 길이, 앞 200자 텍스트 샘플을 표시한다.
- 텍스트가 없는 페이지는 `Text extraction: none`으로 표시한다.
- 분석 결과를 기본적으로 `<파일명>.analysis.json`으로 저장한다.

## GUI Viewer

- `AnoPDF.Rendering`은 Docnet.Core의 PDFium native package를 사용해 PDF 페이지를 BGRA 픽셀 버퍼로 렌더링한다.
- `AnoPDF.Viewer`는 파일 열기, 문서 검사, 전체 페이지 렌더링, 확대율 변경을 UI와 분리한 세션 모델로 제공한다.
- `AnoPDF.Desktop`은 Avalonia 앱이며, 초기 뷰에서 파일 다이얼로그로 PDF 경로를 받고 올바르게 열린 경우에만 PDF 표시 뷰로 전환한다.
- 이후 뷰는 렌더링된 모든 페이지를 하단 방향으로 나열하고, 사용자가 스크롤로 문서 전체를 자유롭게 오가게 한다.
- 창 하단에는 기본 드로잉 세트 툴바를 둔다. 현재는 Pan, Pen, Highlighter, Eraser, HSL/RGB 삼각형 색상환, stroke width 조절 UI만 있는 준비 단계이다.

현재 GUI Viewer의 최소 기능은 다음과 같다.

- Open File: 초기 뷰에서 PDF 파일을 선택하고 문서 정보를 읽는다.
- Direct File Dialog: 초기 뷰의 선택 경로 아래 `Open` 버튼으로 파일 선택 창을 직접 연다.
- Render Document: 문서 전체 페이지를 PDFium으로 다시 렌더링한다.
- Zoom: 확대율을 바꾸고 문서 전체 페이지를 다시 렌더링한다.
- Document Scroll: 전체 페이지를 세로로 쌓아 스크롤로 이동한다.
- Drawing Toolbar: 창 하단에서 기본 드로잉 도구, HSL/RGB 삼각형 색상환, 선 두께를 고른다.

파일 선택기는 Avalonia 창에 연결된 `TopLevel.StorageProvider`에서 열고, 파일 열기를 지원하지 않는 실행 환경에서는 상태 표시줄에 오류를 표시한다.

`Microsoft.WindowsDesktop.App`은 Windows 전용 런타임이라 macOS에서 실행할 수 없다. 따라서 GUI 프로젝트는 `net9.0` Avalonia 앱으로 구성해 macOS arm64에서도 데스크탑 실행이 가능하게 한다.

## 제외 범위

- 텍스트 편집을 하지 않는다.
- 실제 ink stroke 작성, 주석 저장, 드래그 앤 드롭, 도형 그리기 기능을 만들지 않는다.

## 콘솔 실행

```bash
dotnet run --project AnoPDF/AnoPDF.csproj -- sample.pdf
```

JSON 출력 경로를 지정할 수 있다.

```bash
dotnet run --project AnoPDF/AnoPDF.csproj -- sample.pdf --json sample.analysis.json
```

JSON 저장을 생략할 수도 있다.

```bash
dotnet run --project AnoPDF/AnoPDF.csproj -- sample.pdf --no-json
```

## GUI 실행

다음 명령으로 GUI Viewer를 실행한다.

```bash
dotnet run --project AnoPDF.Desktop/AnoPDF.Desktop.csproj
```

## 검증

테스트는 임시 최소 PDF 파일을 생성해 입력 검증, 메타데이터, 페이지 크기, 텍스트 샘플 제한, 텍스트 없는 페이지, JSON 저장, 렌더링 지오메트리, PDFium 렌더링, 파일 열기 세션, 전체 페이지 렌더링, 데스크탑 프로젝트 런타임 구성, 파일 선택기 연결 방식, 스크롤형 문서 뷰 구성, 하단 기본 드로잉 툴바 구성을 확인한다.

```bash
dotnet test AnoPDF.sln
dotnet build AnoPDF.Desktop/AnoPDF.Desktop.csproj -c Release -o build
dotnet build/PdfInspector.dll sample.pdf
```

현재 PdfPig NuGet 패키지는 stable 버전이 없어 `UglyToad.PdfPig` `1.7.0-custom-5` prerelease 패키지를 사용한다.
PDF 렌더링은 PDFium native를 포함하는 `Docnet.Core` `2.6.0`을 사용한다.
GUI는 `Avalonia` `12.0.4`와 `Avalonia.Desktop` `12.0.4`를 사용한다.
