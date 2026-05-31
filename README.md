# PDF Inspector 0.1

PDF Inspector 0.1은 PDF 위에 그리는 편집기를 만들기 전에 PDF 문서를 프로그램이 읽고 설명할 수 있게 만드는 최소 기반이다. 이 단계는 Viewer도 Editor도 아니며, 입력 PDF의 기본 정보와 페이지별 구조를 확인하는 콘솔 분석 도구이다.

## 범위

- PDF 파일 경로를 실행 인자로 받는다.
- 파일 존재 여부와 `.pdf` 확장자를 검사한다.
- PdfPig로 문서를 열어 페이지 수를 읽는다.
- 제목, 작성자, 생성 도구, 생성일, 수정일 등 메타데이터를 표시한다.
- 페이지 번호, 페이지 크기, 회전값, 텍스트 길이, 앞 200자 텍스트 샘플을 표시한다.
- 텍스트가 없는 페이지는 `Text extraction: none`으로 표시한다.
- 분석 결과를 기본적으로 `<파일명>.analysis.json`으로 저장한다.

## 제외 범위

- PDF 렌더링을 하지 않는다.
- 페이지 이미지를 표시하지 않는다.
- 텍스트 편집을 하지 않는다.
- 주석, 드래그 앤 드롭, 도형 그리기 기능을 만들지 않는다.

## 실행

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

## 검증

테스트는 임시 최소 PDF 파일을 생성해 입력 검증, 메타데이터, 페이지 크기, 텍스트 샘플 제한, 텍스트 없는 페이지, JSON 저장을 확인한다.

```bash
dotnet test AnoPDF.sln
dotnet build AnoPDF/AnoPDF.csproj -c Release -o build
dotnet build/PdfInspector.dll sample.pdf
```

현재 PdfPig NuGet 패키지는 stable 버전이 없어 `UglyToad.PdfPig` `1.7.0-custom-5` prerelease 패키지를 사용한다.
