namespace PdfInspector;

public static class PdfInspectorCli
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var arguments = PdfInspectorArguments.Parse(args);
        if (arguments.ShowHelp)
        {
            output.WriteLine(GetUsage());
            return 0;
        }

        if (arguments.ErrorMessage is not null)
        {
            error.WriteLine(arguments.ErrorMessage);
            error.WriteLine(GetUsage());
            return 64;
        }

        try
        {
            var result = new PdfInspectionService().Inspect(arguments.PdfPath!);
            output.Write(ConsoleInspectionFormatter.Format(result));

            if (!arguments.SkipJson)
            {
                var jsonPath = arguments.JsonPath ?? PdfInspectionJsonWriter.GetDefaultOutputPath(result.FilePath);
                PdfInspectionJsonWriter.Save(result, jsonPath);
                output.WriteLine($"JSON: {jsonPath}");
            }

            return 0;
        }
        catch (PdfInspectionException exception)
        {
            error.WriteLine($"Error: {exception.Message}");
            return GetExitCode(exception.Failure);
        }
    }

    private static int GetExitCode(PdfInspectionFailure failure)
    {
        return failure switch
        {
            PdfInspectionFailure.EmptyPath => 64,
            PdfInspectionFailure.FileNotFound => 66,
            PdfInspectionFailure.NotPdfFile => 65,
            PdfInspectionFailure.FileCannotBeOpened => 74,
            PdfInspectionFailure.EncryptedPdf => 77,
            PdfInspectionFailure.InvalidPdf => 65,
            _ => 1
        };
    }

    private static string GetUsage()
    {
        return """
               Usage:
                 PdfInspector <file.pdf> [--json <output.json>]
                 PdfInspector <file.pdf> --no-json

               PDF Inspector 0.1 reads document facts, metadata, page summaries, and short text samples.
               It does not render, edit, annotate, or save PDF pages.
               """;
    }
}

internal sealed record PdfInspectorArguments(
    string? PdfPath,
    string? JsonPath,
    bool SkipJson,
    bool ShowHelp,
    string? ErrorMessage)
{
    public static PdfInspectorArguments Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new PdfInspectorArguments(null, null, false, false, "A PDF path argument is required.");
        }

        string? pdfPath = null;
        string? jsonPath = null;
        var skipJson = false;

        for (var i = 0; i < args.Count; i++)
        {
            var argument = args[i];
            if (argument is "--help" or "-h")
            {
                return new PdfInspectorArguments(null, null, false, true, null);
            }

            if (argument == "--no-json")
            {
                skipJson = true;
                continue;
            }

            if (argument == "--json")
            {
                if (i + 1 >= args.Count)
                {
                    return new PdfInspectorArguments(null, null, false, false, "--json requires an output path.");
                }

                jsonPath = args[++i];
                continue;
            }

            if (argument.StartsWith("-", StringComparison.Ordinal))
            {
                return new PdfInspectorArguments(null, null, false, false, $"Unknown option: {argument}");
            }

            if (pdfPath is not null)
            {
                return new PdfInspectorArguments(null, null, false, false, "Only one PDF path can be inspected at a time.");
            }

            pdfPath = argument;
        }

        if (pdfPath is null)
        {
            return new PdfInspectorArguments(null, null, false, false, "A PDF path argument is required.");
        }

        if (skipJson && jsonPath is not null)
        {
            return new PdfInspectorArguments(null, null, false, false, "--json and --no-json cannot be used together.");
        }

        return new PdfInspectorArguments(pdfPath, jsonPath, skipJson, false, null);
    }
}
