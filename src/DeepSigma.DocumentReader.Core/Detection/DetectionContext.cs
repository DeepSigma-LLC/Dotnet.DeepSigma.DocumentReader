using System.Text;

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
        PrefixText = DecodePrefix(prefix.Span);
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

    private static string DecodePrefix(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        // Honor a UTF-8/UTF-16 BOM when present; otherwise decode as UTF-8 without throwing.
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes[3..]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes[2..]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        }

        return Encoding.UTF8.GetString(bytes);
    }
}
