namespace DeepSigma.DocumentReader;

/// <summary>
/// An input document to be read. A source can be created from a file path, an existing
/// stream, or an in-memory byte array.
/// </summary>
/// <remarks>
/// <para>
/// The underlying <see cref="Stream"/> is not guaranteed to be seekable; the reading
/// pipeline buffers it when seeking is required.
/// </para>
/// <para>
/// Disposing the source disposes the underlying stream only when the source owns it
/// (see <see cref="OwnsStream"/>): <see cref="FromFile"/> and <see cref="FromBytes"/>
/// own their streams, while <see cref="FromStream"/> does not.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using DocumentSource fromFile = DocumentSource.FromFile("report.docx");
/// using DocumentSource fromBytes = DocumentSource.FromBytes(bytes, "data.json", "application/json");
/// DocumentSource fromStream = DocumentSource.FromStream(httpStream, "page.html", "text/html");
/// </code>
/// </example>
public sealed class DocumentSource : IDisposable
{
    private DocumentSource(Stream stream, string? fileName, string? contentType, bool ownsStream)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        FileName = fileName;
        ContentType = contentType;
        OwnsStream = ownsStream;
    }

    /// <summary>The original file name, if known. Used as a hint for type detection.</summary>
    public string? FileName { get; }

    /// <summary>The explicit MIME content type, if supplied. Used as a hint for type detection.</summary>
    public string? ContentType { get; }

    /// <summary>The document content. May or may not be seekable.</summary>
    public Stream Stream { get; }

    /// <summary>
    /// Whether disposing this source should also dispose <see cref="Stream"/>.
    /// </summary>
    public bool OwnsStream { get; }

    /// <summary>
    /// Creates a source from a file on disk. The file is opened immediately for reading;
    /// the resulting source owns the stream and closes it on disposal.
    /// </summary>
    public static DocumentSource FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        // Content type is intentionally left unset; the type detector infers it from the
        // file name extension and content signals.
        return new DocumentSource(stream, Path.GetFileName(path), contentType: null, ownsStream: true);
    }

    /// <summary>
    /// Creates a source from an existing stream. The caller retains ownership of the
    /// stream; disposing the source does not dispose it.
    /// </summary>
    public static DocumentSource FromStream(Stream stream, string? fileName = null, string? contentType = null)
        => new(stream, fileName, contentType, ownsStream: false);

    /// <summary>
    /// Creates a source from an in-memory byte array. The resulting source owns the
    /// wrapping stream and closes it on disposal.
    /// </summary>
    public static DocumentSource FromBytes(byte[] bytes, string? fileName = null, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new DocumentSource(new MemoryStream(bytes, writable: false), fileName, contentType, ownsStream: true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (OwnsStream)
        {
            Stream.Dispose();
        }
    }
}
