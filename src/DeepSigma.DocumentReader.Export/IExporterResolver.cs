namespace DeepSigma.DocumentReader.Export;

/// <summary>Resolves an <see cref="IDocumentResultExporter"/> by its format name.</summary>
public interface IExporterResolver
{
    /// <summary>The format names this resolver can produce (e.g. <c>text</c>, <c>markdown</c>, <c>json</c>).</summary>
    IReadOnlyCollection<string> SupportedFormats { get; }

    /// <summary>Returns the exporter for <paramref name="format"/>, or <see langword="null"/> if unsupported.</summary>
    IDocumentResultExporter? Resolve(string format);
}

/// <summary>Default resolver over a fixed set of exporters, matched case-insensitively by format.</summary>
public sealed class ExporterResolver : IExporterResolver
{
    private readonly Dictionary<string, IDocumentResultExporter> _exporters;

    /// <summary>Creates a resolver over the supplied exporters.</summary>
    public ExporterResolver(IEnumerable<IDocumentResultExporter> exporters)
    {
        ArgumentNullException.ThrowIfNull(exporters);
        _exporters = exporters.ToDictionary(e => e.Format, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Creates a resolver over the built-in text, Markdown, and JSON exporters.</summary>
    public static ExporterResolver CreateDefault()
        => new([new TextResultExporter(), new MarkdownResultExporter(), new JsonResultExporter()]);

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedFormats => _exporters.Keys;

    /// <inheritdoc />
    public IDocumentResultExporter? Resolve(string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return _exporters.GetValueOrDefault(format);
    }
}
