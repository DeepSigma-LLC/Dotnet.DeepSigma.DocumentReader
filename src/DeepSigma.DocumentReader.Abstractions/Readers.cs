namespace DeepSigma.DocumentReader;

/// <summary>
/// The primary entry point for reading documents. The composite implementation routes
/// to the appropriate format reader; consumers normally depend on this interface only.
/// </summary>
public interface IDocumentReader
{
    /// <summary>Returns whether this reader can handle the given source.</summary>
    bool CanRead(DocumentSource source);

    /// <summary>Reads the document, returning a unified result.</summary>
    Task<DocumentReadResult> ReadAsync(
        DocumentSource source,
        DocumentReadOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A reader for a specific family of formats. Format packages implement this; the
/// composite reader selects among registered instances using <see cref="GetConfidence"/>.
/// </summary>
public interface IFormatDocumentReader : IDocumentReader
{
    /// <summary>The document kinds this reader can handle.</summary>
    IReadOnlyCollection<DocumentKind> SupportedKinds { get; }

    /// <summary>
    /// Returns this reader's confidence (0–100) for handling the source given a detection
    /// result, used to break ties when multiple readers can read the same source.
    /// </summary>
    int GetConfidence(DocumentSource source, DocumentTypeDetectionResult detectionResult);
}

/// <summary>Exposes all registered format readers.</summary>
public interface IDocumentReaderProvider
{
    /// <summary>The registered format readers.</summary>
    IReadOnlyCollection<IFormatDocumentReader> Readers { get; }
}
