namespace DeepSigma.DocumentReader;

/// <summary>
/// A single candidate produced during type detection, attributing a confidence to the
/// signal that suggested it.
/// </summary>
/// <param name="Kind">The suggested document kind.</param>
/// <param name="Confidence">Confidence on a 0–100 scale.</param>
/// <param name="Signal">A short name for the signal that produced this candidate (e.g. <c>extension</c>, <c>magic-bytes</c>).</param>
public sealed record DocumentTypeCandidate(DocumentKind Kind, int Confidence, string Signal);

/// <summary>
/// The result of detecting a document's type, including the winning kind, its confidence,
/// and the full list of candidates considered (useful for diagnostics).
/// </summary>
public sealed record DocumentTypeDetectionResult
{
    /// <summary>The most likely document kind.</summary>
    public required DocumentKind Kind { get; init; }

    /// <summary>Confidence in <see cref="Kind"/> on a 0–100 scale.</summary>
    public required int Confidence { get; init; }

    /// <summary>The resolved or supplied MIME content type, if any.</summary>
    public string? ContentType { get; init; }

    /// <summary>The file extension that informed detection, if any (including the leading dot).</summary>
    public string? Extension { get; init; }

    /// <summary>All candidates considered, ordered by descending confidence.</summary>
    public IReadOnlyList<DocumentTypeCandidate> Candidates { get; init; } = [];
}

/// <summary>
/// Detects the <see cref="DocumentKind"/> of a source by combining several signals
/// (content type, extension, magic bytes, container inspection, content sniffing).
/// </summary>
public interface IDocumentTypeDetector
{
    /// <summary>
    /// Detects the document type. Implementations should read only a small prefix of the
    /// source and must not consume the entire stream.
    /// </summary>
    ValueTask<DocumentTypeDetectionResult> DetectAsync(
        DocumentSource source,
        CancellationToken cancellationToken = default);
}
