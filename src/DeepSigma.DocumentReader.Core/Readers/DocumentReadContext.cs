using DeepSigma.DocumentReader.Core.Streams;

namespace DeepSigma.DocumentReader.Core.Readers;

/// <summary>
/// The working state handed to a format reader: a seekable stream over the (already
/// size-checked) content, the source hints, the effective options, and a warning sink.
/// </summary>
public sealed class DocumentReadContext
{
    private readonly BufferedDocument _buffered;
    private readonly List<DocumentWarning> _warnings = [];

    internal DocumentReadContext(DocumentSource source, BufferedDocument buffered, DocumentReadOptions options)
    {
        Source = source;
        _buffered = buffered;
        Options = options;
    }

    /// <summary>The original source.</summary>
    public DocumentSource Source { get; }

    /// <summary>The effective read options.</summary>
    public DocumentReadOptions Options { get; }

    /// <summary>A seekable stream over the content, positioned at the start.</summary>
    public Stream Stream => _buffered.Stream;

    /// <summary>The content length in bytes.</summary>
    public long SizeBytes => _buffered.Length;

    /// <summary>Warnings accumulated during the read.</summary>
    public IReadOnlyList<DocumentWarning> Warnings => _warnings;

    /// <summary>Repositions the content stream at the beginning.</summary>
    public void Rewind() => _buffered.Rewind();

    /// <summary>Records a non-fatal problem.</summary>
    public void AddWarning(string code, string message, DocumentLocation? location = null, Exception? exception = null)
        => _warnings.Add(new DocumentWarning
        {
            Code = code,
            Message = message,
            Location = location,
            Exception = exception,
        });

    /// <summary>Builds source info for the result, using the detected/known kind.</summary>
    public DocumentSourceInfo CreateSourceInfo(DocumentKind kind) => new()
    {
        FileName = Source.FileName,
        ContentType = Source.ContentType,
        SizeBytes = SizeBytes,
        DetectedKind = kind,
    };
}
