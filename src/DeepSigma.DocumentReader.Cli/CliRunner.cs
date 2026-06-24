using System.Text;
using System.Text.Json;
using DeepSigma.DocumentReader.Export;

namespace DeepSigma.DocumentReader.Cli;

/// <summary>
/// The CLI's working logic, decoupled from argument parsing so it can be driven directly in
/// tests. Each method returns a process exit code.
/// </summary>
public sealed class CliRunner(
    IDocumentReader reader,
    IDocumentTypeDetector detector,
    IExporterResolver exporters,
    TextWriter output,
    TextWriter error)
{
    /// <summary>Exit code: success.</summary>
    public const int ExitSuccess = 0;

    /// <summary>Exit code: a usage or I/O error.</summary>
    public const int ExitError = 1;

    /// <summary>Exit code: the document type or requested format is unsupported.</summary>
    public const int ExitUnsupported = 2;

    /// <summary>Exit code: extraction produced warnings and <c>--strict</c> was set.</summary>
    public const int ExitWarnings = 3;

    /// <summary>Detects and prints the type of a document.</summary>
    public async Task<int> DetectAsync(string path, bool json, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            await error.WriteLineAsync($"File not found: {path}").ConfigureAwait(false);
            return ExitError;
        }

        using var source = DocumentSource.FromFile(path);
        DocumentTypeDetectionResult detection = await detector.DetectAsync(source, cancellationToken).ConfigureAwait(false);

        if (json)
        {
            await output.WriteLineAsync(JsonSerializer.Serialize(detection, JsonOutputOptions)).ConfigureAwait(false);
        }
        else
        {
            await output.WriteLineAsync($"Kind:       {detection.Kind}").ConfigureAwait(false);
            await output.WriteLineAsync($"Confidence: {detection.Confidence}").ConfigureAwait(false);
            await output.WriteLineAsync($"ContentType:{detection.ContentType}").ConfigureAwait(false);
            await output.WriteLineAsync("Candidates:").ConfigureAwait(false);
            foreach (DocumentTypeCandidate candidate in detection.Candidates)
            {
                await output.WriteLineAsync($"  {candidate.Kind} ({candidate.Confidence}) via {candidate.Signal}").ConfigureAwait(false);
            }
        }

        return ExitSuccess;
    }

    /// <summary>Reads a document and exports it in the requested format to a file or stdout.</summary>
    public async Task<int> ExtractAsync(
        string path,
        string format,
        string? outputPath,
        DocumentReadOptions options,
        bool strict,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            await error.WriteLineAsync($"File not found: {path}").ConfigureAwait(false);
            return ExitError;
        }

        IDocumentResultExporter? exporter = exporters.Resolve(format);
        if (exporter is null)
        {
            await error.WriteLineAsync(
                $"Unsupported format '{format}'. Supported: {string.Join(", ", exporters.SupportedFormats)}.").ConfigureAwait(false);
            return ExitUnsupported;
        }

        DocumentReadResult result;
        try
        {
            using var source = DocumentSource.FromFile(path);
            result = await reader.ReadAsync(source, options, cancellationToken).ConfigureAwait(false);
        }
        catch (UnsupportedDocumentTypeException ex)
        {
            await error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ExitUnsupported;
        }

        if (outputPath is null)
        {
            await using var stdout = Console.OpenStandardOutput();
            await exporter.ExportAsync(result, stdout, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await using var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await exporter.ExportAsync(result, file, cancellationToken).ConfigureAwait(false);
        }

        if (strict && result.Warnings.Count > 0)
        {
            await error.WriteLineAsync($"{result.Warnings.Count} warning(s) raised.").ConfigureAwait(false);
            return ExitWarnings;
        }

        return ExitSuccess;
    }

    /// <summary>Reads a document and prints a structured summary of what it contains.</summary>
    public async Task<int> InspectAsync(string path, DocumentReadOptions options, bool json, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            await error.WriteLineAsync($"File not found: {path}").ConfigureAwait(false);
            return ExitError;
        }

        DocumentReadResult result;
        try
        {
            using var source = DocumentSource.FromFile(path);
            result = await reader.ReadAsync(source, options, cancellationToken).ConfigureAwait(false);
        }
        catch (UnsupportedDocumentTypeException ex)
        {
            await error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ExitUnsupported;
        }

        if (json)
        {
            IDocumentResultExporter? jsonExporter = exporters.Resolve("json");
            if (jsonExporter is not null)
            {
                await using var stdout = Console.OpenStandardOutput();
                await jsonExporter.ExportAsync(result, stdout, cancellationToken).ConfigureAwait(false);
                return ExitSuccess;
            }
        }

        await output.WriteLineAsync($"File:     {result.Source.FileName}").ConfigureAwait(false);
        await output.WriteLineAsync($"Kind:     {result.Kind}").ConfigureAwait(false);
        await output.WriteLineAsync($"Size:     {result.Source.SizeBytes} bytes").ConfigureAwait(false);
        await output.WriteLineAsync($"Quality:  {result.Quality}").ConfigureAwait(false);
        await output.WriteLineAsync($"Sections: {result.Sections.Count}").ConfigureAwait(false);
        await output.WriteLineAsync($"Tables:   {result.Tables.Count}").ConfigureAwait(false);
        await output.WriteLineAsync($"Warnings: {result.Warnings.Count}").ConfigureAwait(false);
        foreach (DocumentWarning warning in result.Warnings)
        {
            await output.WriteLineAsync($"  [{warning.Code}] {warning.Message}").ConfigureAwait(false);
        }

        return ExitSuccess;
    }

    /// <summary>Reads every file in a directory, exporting each and writing an NDJSON manifest.</summary>
    public async Task<int> BatchAsync(
        string inputDirectory,
        string outputDirectory,
        string format,
        bool recursive,
        DocumentReadOptions options,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(inputDirectory))
        {
            await error.WriteLineAsync($"Directory not found: {inputDirectory}").ConfigureAwait(false);
            return ExitError;
        }

        IDocumentResultExporter? exporter = exporters.Resolve(format);
        if (exporter is null)
        {
            await error.WriteLineAsync($"Unsupported format '{format}'.").ConfigureAwait(false);
            return ExitUnsupported;
        }

        Directory.CreateDirectory(outputDirectory);
        string extension = format == "json" ? ".json" : format == "markdown" ? ".md" : ".txt";

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        await using var manifest = new FileStream(
            Path.Combine(outputDirectory, "manifest.ndjson"), FileMode.Create, FileAccess.Write, FileShare.None);
        await using var manifestWriter = new StreamWriter(manifest, new UTF8Encoding(false));

        int failures = 0;
        foreach (string file in Directory.EnumerateFiles(inputDirectory, "*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Mirror the input's relative path and keep its original extension so that files
            // with the same stem (report.txt vs report.pdf) or the same name in different
            // subdirectories do not overwrite each other.
            string relative = Path.GetRelativePath(inputDirectory, file);
            try
            {
                using var source = DocumentSource.FromFile(file);
                DocumentReadResult result = await reader.ReadAsync(source, options, cancellationToken).ConfigureAwait(false);

                string outputFile = Path.Combine(outputDirectory, relative + extension);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                await using (var outStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await exporter.ExportAsync(result, outStream, cancellationToken).ConfigureAwait(false);
                }

                await WriteManifestLineAsync(manifestWriter, relative, result.Kind.ToString(), result.Quality.ToString(), result.Warnings.Count, success: true).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures++;
                await error.WriteLineAsync($"Failed: {relative}: {ex.Message}").ConfigureAwait(false);
                await WriteManifestLineAsync(manifestWriter, relative, "Unknown", "Failed", 0, success: false).ConfigureAwait(false);
            }
        }

        return failures > 0 ? ExitError : ExitSuccess;
    }

    private static readonly JsonSerializerOptions JsonOutputOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static async Task WriteManifestLineAsync(
        TextWriter writer, string file, string kind, string quality, int warnings, bool success)
    {
        var line = JsonSerializer.Serialize(new
        {
            file,
            kind,
            quality,
            warnings,
            success,
        });
        await writer.WriteLineAsync(line).ConfigureAwait(false);
    }
}
