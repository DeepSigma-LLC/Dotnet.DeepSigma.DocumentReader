namespace DeepSigma.DocumentReader;

/// <summary>Summary information about the source a result was produced from.</summary>
public sealed class DocumentSourceInfo
{
    /// <summary>The source file name, if known.</summary>
    public string? FileName { get; init; }

    /// <summary>The resolved MIME content type, if known.</summary>
    public string? ContentType { get; init; }

    /// <summary>The source size in bytes, if known.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>The detected document kind.</summary>
    public DocumentKind DetectedKind { get; init; }
}

/// <summary>Common document metadata, plus an open bag for format-specific entries.</summary>
public sealed class DocumentMetadata
{
    /// <summary>The document title, if any.</summary>
    public string? Title { get; init; }

    /// <summary>The document author, if any.</summary>
    public string? Author { get; init; }

    /// <summary>When the document was created, if known.</summary>
    public DateTimeOffset? CreatedUtc { get; init; }

    /// <summary>When the document was last modified, if known.</summary>
    public DateTimeOffset? ModifiedUtc { get; init; }

    /// <summary>The document language (BCP-47), if known.</summary>
    public string? Language { get; init; }

    /// <summary>The page count, if applicable.</summary>
    public int? PageCount { get; init; }

    /// <summary>Additional, format-specific metadata entries.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; }
        = new Dictionary<string, string>();
}

/// <summary>
/// The unified result of reading a document: a text projection plus structured
/// extraction details and any warnings.
/// </summary>
public sealed class DocumentReadResult
{
    /// <summary>Information about the source the result came from.</summary>
    public required DocumentSourceInfo Source { get; init; }

    /// <summary>The detected document kind.</summary>
    public required DocumentKind Kind { get; init; }

    /// <summary>A flat text projection of the whole document, if produced.</summary>
    public string? Text { get; init; }

    /// <summary>Pages, for paginated formats.</summary>
    public IReadOnlyList<DocumentPage> Pages { get; init; } = [];

    /// <summary>Sections, for formats with a heading hierarchy.</summary>
    public IReadOnlyList<DocumentSection> Sections { get; init; } = [];

    /// <summary>Tables extracted from the document.</summary>
    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];

    /// <summary>Images extracted from or described in the document.</summary>
    public IReadOnlyList<DocumentImage> Images { get; init; } = [];

    /// <summary>Attachments or embedded files.</summary>
    public IReadOnlyList<DocumentAttachment> Attachments { get; init; } = [];

    /// <summary>Document metadata.</summary>
    public DocumentMetadata Metadata { get; init; } = new();

    /// <summary>Non-fatal problems encountered while reading.</summary>
    public IReadOnlyList<DocumentWarning> Warnings { get; init; } = [];

    /// <summary>The assessed extraction quality.</summary>
    public ExtractionQuality Quality { get; init; } = ExtractionQuality.Unknown;

    /// <summary>Format-specific features attached by the reader.</summary>
    public IReadOnlyList<IDocumentFeature> Features { get; init; } = [];
}
