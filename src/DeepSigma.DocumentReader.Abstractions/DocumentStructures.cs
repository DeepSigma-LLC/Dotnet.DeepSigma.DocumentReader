namespace DeepSigma.DocumentReader;

/// <summary>A contiguous run of text, optionally located within its source.</summary>
public sealed class TextBlock
{
    /// <summary>The text content of the block.</summary>
    public required string Text { get; init; }

    /// <summary>Where the block originated, if known.</summary>
    public DocumentLocation? Location { get; init; }
}

/// <summary>A single page of a paginated document (PDF, Word).</summary>
public sealed class DocumentPage
{
    /// <summary>The 1-based page number.</summary>
    public required int PageNumber { get; init; }

    /// <summary>The page's text projection, if extracted.</summary>
    public string? Text { get; init; }

    /// <summary>The text blocks on the page.</summary>
    public IReadOnlyList<TextBlock> Blocks { get; init; } = [];

    /// <summary>Tables found on the page.</summary>
    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];

    /// <summary>Images found on the page.</summary>
    public IReadOnlyList<DocumentImage> Images { get; init; } = [];
}

/// <summary>
/// A logical section of a document, forming a tree via <see cref="Children"/>.
/// Built from heading hierarchies (Markdown, Word, HTML).
/// </summary>
public sealed class DocumentSection
{
    /// <summary>The section heading/title, if any.</summary>
    public string? Title { get; init; }

    /// <summary>The heading level (1 = top level).</summary>
    public int Level { get; init; }

    /// <summary>The section's own text (excluding child sections), if extracted.</summary>
    public string? Text { get; init; }

    /// <summary>Where the section originated, if known.</summary>
    public DocumentLocation? Location { get; init; }

    /// <summary>Nested child sections.</summary>
    public IReadOnlyList<DocumentSection> Children { get; init; } = [];
}

/// <summary>A tabular structure extracted from a document.</summary>
public sealed class DocumentTable
{
    /// <summary>The table name or caption, if any.</summary>
    public string? Name { get; init; }

    /// <summary>Column header texts, when the source distinguishes a header row.</summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>Where the table originated, if known.</summary>
    public DocumentLocation? Location { get; init; }

    /// <summary>The table's rows.</summary>
    public IReadOnlyList<DocumentTableRow> Rows { get; init; } = [];

    /// <summary>Confidence in the extracted structure, 0–1, when the reader can estimate it.</summary>
    public double? Confidence { get; init; }
}

/// <summary>A single row within a <see cref="DocumentTable"/>.</summary>
public sealed class DocumentTableRow
{
    /// <summary>The 0-based row index.</summary>
    public int RowIndex { get; init; }

    /// <summary>The cells in the row.</summary>
    public IReadOnlyList<DocumentTableCell> Cells { get; init; } = [];
}

/// <summary>A single cell within a <see cref="DocumentTableRow"/>.</summary>
public sealed class DocumentTableCell
{
    /// <summary>The 0-based row index.</summary>
    public int RowIndex { get; init; }

    /// <summary>The 0-based column index.</summary>
    public int ColumnIndex { get; init; }

    /// <summary>The cell's normalized text projection.</summary>
    public string? Text { get; init; }

    /// <summary>The cell's typed value, when the reader can determine a CLR type.</summary>
    public object? RawValue { get; init; }

    /// <summary>Where the cell originated, if known.</summary>
    public DocumentLocation? Location { get; init; }
}

/// <summary>Metadata describing an image embedded in a document.</summary>
public sealed class DocumentImage
{
    /// <summary>The image's MIME content type, if known.</summary>
    public string? ContentType { get; init; }

    /// <summary>The image's size in bytes, if known.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Where the image originated, if known.</summary>
    public DocumentLocation? Location { get; init; }
}

/// <summary>
/// An attachment or embedded file within a document (e.g. an email attachment).
/// </summary>
public abstract class DocumentAttachment
{
    /// <summary>The attachment's file name, if known.</summary>
    public string? FileName { get; init; }

    /// <summary>The attachment's MIME content type, if known.</summary>
    public string? ContentType { get; init; }

    /// <summary>The attachment's size in bytes, if known.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Where the attachment originated, if known.</summary>
    public DocumentLocation? Location { get; init; }

    /// <summary>Opens a readable stream over the attachment's content.</summary>
    public abstract Stream OpenReadStream();

    /// <summary>Wraps the attachment as a <see cref="DocumentSource"/> for recursive reading.</summary>
    public DocumentSource AsDocumentSource()
        => DocumentSource.FromStream(OpenReadStream(), FileName, ContentType);
}

/// <summary>A heading extracted from a document (Markdown, HTML, …).</summary>
/// <param name="Level">The heading level (1 = top level).</param>
/// <param name="Text">The heading text.</param>
public sealed record DocumentHeading(int Level, string Text);

/// <summary>A link or image reference.</summary>
/// <param name="Text">The link/alt text, if any.</param>
/// <param name="Url">The target URL.</param>
/// <param name="IsImage">Whether the reference is an image.</param>
public sealed record DocumentLink(string? Text, string Url, bool IsImage);

/// <summary>An attachment whose content is held in memory as a byte array.</summary>
public sealed class ByteArrayDocumentAttachment : DocumentAttachment
{
    private readonly byte[] _content;

    /// <summary>Creates an attachment over the supplied content.</summary>
    public ByteArrayDocumentAttachment(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
        SizeBytes = content.Length;
    }

    /// <inheritdoc />
    public override Stream OpenReadStream() => new MemoryStream(_content, writable: false);
}
