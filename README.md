# AnoPDF

AnoPDF는 PDF 위에 그리는 편집기로 가기 전, PDF를 먼저 읽고 설명하고 렌더링하는 기반을 단계적으로 만든다. 현재 구성은 콘솔 기반 `PDF Inspector 0.1`과 WPF 기반 첫 GUI Viewer이다.

## PDF Inspector 0.1

- PDF 파일 경로를 실행 인자로 받는다.
- 파일 존재 여부와 `.pdf` 확장자를 검사한다.
- PdfPig로 문서를 열어 페이지 수를 읽는다.
- 제목, 작성자, 생성 도구, 생성일, 수정일 등 메타데이터를 표시한다.
- 페이지 번호, 페이지 크기, 회전값, 텍스트 길이, 앞 200자 텍스트 샘플을 표시한다.
- 텍스트가 없는 페이지는 `Text extraction: none`으로 표시한다.
- 분석 결과를 기본적으로 `<파일명>.analysis.json`으로 저장한다.

## GUI Viewer

- `AnoPDF.Rendering`은 PDFiumSharpV2와 PDFium Windows native package를 사용해 PDF 페이지를 BGRA 픽셀 버퍼로 렌더링한다.
- `AnoPDF.Viewer`는 파일 열기, 문서 검사, 첫 페이지 렌더링, 페이지 이동, 확대율 변경을 UI와 분리한 세션 모델로 제공한다.
- `AnoPDF.Desktop`은 WPF 앱이며, `OpenFileDialog`로 PDF를 열고 렌더링된 페이지를 `Image`에 표시한다.

현재 GUI Viewer의 최소 기능은 다음과 같다.

- Open File: PDF 파일을 선택하고 문서 정보를 읽는다.
- Render Page: 현재 페이지를 PDFium으로 다시 렌더링한다.
- Zoom: 확대율을 바꾸고 현재 페이지를 다시 렌더링한다.
- Page Navigation: 이전/다음 페이지로 이동한다.

WPF는 Windows 전용 UI 기술이다. 이 저장소는 macOS에서도 컴파일 검증을 할 수 있도록 `net9.0-windows`와 `EnableWindowsTargeting`을 사용하지만, 실제 GUI 실행은 Windows에서 수행해야 한다.

## 제외 범위

- 텍스트 편집을 하지 않는다.
- 주석, 드래그 앤 드롭, 도형 그리기 기능을 만들지 않는다.

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

Windows에서 다음 명령으로 WPF Viewer를 실행한다.

```bash
dotnet run --project AnoPDF.Desktop/AnoPDF.Desktop.csproj
```

## 검증

테스트는 임시 최소 PDF 파일을 생성해 입력 검증, 메타데이터, 페이지 크기, 텍스트 샘플 제한, 텍스트 없는 페이지, JSON 저장, 렌더링 지오메트리, 파일 열기 세션, 페이지 범위 검증을 확인한다.

```bash
dotnet test AnoPDF.sln
dotnet build AnoPDF.Desktop/AnoPDF.Desktop.csproj -c Release -o build
dotnet build/PdfInspector.dll sample.pdf
```

현재 PdfPig NuGet 패키지는 stable 버전이 없어 `UglyToad.PdfPig` `1.7.0-custom-5` prerelease 패키지를 사용한다.
PDF 렌더링은 `PDFiumSharpV2` `1.1.4`와 `PDFium.WindowsV2` `1.1.4`를 사용한다.
