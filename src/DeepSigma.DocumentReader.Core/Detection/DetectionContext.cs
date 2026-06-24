using DeepSigma.DocumentReader.Core.Text;

namespace DeepSigma.DocumentReader.Core.Detection;

/// <summary>
/// The inputs available to detection signals: source hints plus a decoded prefix of the
/// content. Signals contribute scored candidates via <see cref="AddCandidate"/>.
/// </summary>
public sealed class DetectionContext
{
    private readonly List<DocumentTypeCandidate> _candidates = [];

    internal DetectionContext(string? fileName, string? contentType, ReadOnlyMemory<byte> prefix)
    {
        FileName = fileName;
        ContentType = contentType;
        Extension = string.IsNullOrEmpty(fileName) ? null : Path.GetExtension(fileName);
        Prefix = prefix;
        PrefixText = TextContent.DecodeBomAware(prefix.Span);
    }

    /// <summary>The source file name, if known.</summary>
    public string? FileName { get; }

    /// <summary>The explicit MIME content type supplied with the source, if any.</summary>
    public string? ContentType { get; }

    /// <summary>The file extension (including the leading dot), if a file name was supplied.</summary>
    public string? Extension { get; }

    /// <summary>The raw leading bytes of the content.</summary>
    public ReadOnlyMemory<byte> Prefix { get; }

    /// <summary>The prefix decoded as text (best-effort UTF-8, BOM-aware).</summary>
    public string PrefixText { get; }

    /// <summary>Candidates contributed so far.</summary>
    public IReadOnlyList<DocumentTypeCandidate> Candidates => _candidates;

    /// <summary>Records a candidate kind with a confidence (0–100) and the contributing signal name.</summary>
    public void AddCandidate(DocumentKind kind, int confidence, string signal)
    {
        if (kind == DocumentKind.Unknown)
        {
            return;
        }

        _candidates.Add(new DocumentTypeCandidate(kind, Math.Clamp(confidence, 0, 100), signal));
    }
}
