namespace DeepSigma.DocumentReader;

/// <summary>
/// Serializes a <see cref="DocumentReadResult"/> to an output stream in a particular
/// format (plain text, Markdown, JSON, …).
/// </summary>
/// <example>
/// <code>
/// IDocumentResultExporter exporter = ExporterResolver.CreateDefault().Resolve("markdown")!;
/// await using var output = File.Create("invoice.md");
/// await exporter.ExportAsync(result, output);
/// </code>
/// </example>
public interface IDocumentResultExporter
{
    /// <summary>A short format name this exporter handles (e.g. <c>markdown</c>).</summary>
    string Format { get; }

    /// <summary>The MIME content type this exporter produces.</summary>
    string ContentType { get; }

    /// <summary>Writes <paramref name="result"/> to <paramref name="output"/>.</summary>
    Task ExportAsync(
        DocumentReadResult result,
        Stream output,
        CancellationToken cancellationToken = default);
}
