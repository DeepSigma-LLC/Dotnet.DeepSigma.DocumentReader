using System.IO.Compression;

namespace DeepSigma.DocumentReader.Core.Detection;

/// <summary>
/// Combines a set of <see cref="IDetectionSignal"/>s into a single detection result. Reads
/// only a small prefix of the source and picks the highest-confidence candidate, summing a
/// small agreement bonus when multiple signals concur on the same kind.
/// </summary>
public sealed class CompositeDocumentTypeDetector : IDocumentTypeDetector
{
    /// <summary>The number of leading bytes inspected for content-based signals.</summary>
    public const int PrefixByteCount = 4096;

    private readonly IReadOnlyList<IDetectionSignal> _signals;

    /// <summary>Creates a detector using the supplied signals.</summary>
    public CompositeDocumentTypeDetector(IEnumerable<IDetectionSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        _signals = signals.ToList();
    }

    /// <summary>Creates a detector using the default built-in signals.</summary>
    public static CompositeDocumentTypeDetector CreateDefault()
        => new(
        [
            new ContentTypeSignal(),
            new ExtensionSignal(),
            new MagicBytesSignal(),
            new ContentSniffSignal(),
        ]);

    /// <inheritdoc />
    public async ValueTask<DocumentTypeDetectionResult> DetectAsync(
        DocumentSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        byte[] prefix = await StreamPeek.ReadPrefixAsync(source.Stream, PrefixByteCount, cancellationToken)
            .ConfigureAwait(false);

        var context = new DetectionContext(source.FileName, source.ContentType, prefix);
        foreach (var signal in _signals)
        {
            signal.Evaluate(context);
        }

        await RefineOoxmlAsync(source, context, cancellationToken).ConfigureAwait(false);

        return Combine(context);
    }

    /// <summary>
    /// When the source is a ZIP container (by magic bytes or an Office extension), inspects
    /// <c>[Content_Types].xml</c> to distinguish DOCX / XLSX / PPTX. Requires a seekable
    /// stream; the position is restored afterwards.
    /// </summary>
    private static async Task RefineOoxmlAsync(DocumentSource source, DetectionContext context, CancellationToken cancellationToken)
    {
        if (!IsZip(context.Prefix.Span) && !IsOfficeExtension(context.Extension))
        {
            return;
        }

        Stream stream = source.Stream;
        if (!stream.CanSeek)
        {
            return;
        }

        long position = stream.Position;
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            ZipArchiveEntry? entry = archive.GetEntry("[Content_Types].xml");
            if (entry is null)
            {
                return;
            }

            using var reader = new StreamReader(entry.Open());
            string contentTypes = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            DocumentKind kind = MapOoxmlContentTypes(contentTypes);
            if (kind != DocumentKind.Unknown)
            {
                context.AddCandidate(kind, 95, "ooxml-container");
            }
        }
        catch (InvalidDataException)
        {
            // Not a valid ZIP/OOXML container; leave the existing candidates as-is.
        }
        finally
        {
            stream.Seek(position, SeekOrigin.Begin);
        }
    }

    private static DocumentKind MapOoxmlContentTypes(string contentTypes)
    {
        if (contentTypes.Contains("wordprocessingml.document", StringComparison.Ordinal))
        {
            return DocumentKind.WordDocument;
        }

        if (contentTypes.Contains("spreadsheetml.sheet", StringComparison.Ordinal))
        {
            return DocumentKind.Spreadsheet;
        }

        if (contentTypes.Contains("presentationml.presentation", StringComparison.Ordinal)
            || contentTypes.Contains("presentationml.slideshow", StringComparison.Ordinal))
        {
            return DocumentKind.Presentation;
        }

        return DocumentKind.Unknown;
    }

    private static bool IsZip(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04;

    private static bool IsOfficeExtension(string? extension)
        => FileFormatRegistry.KindFromExtension(extension) is DocumentKind.WordDocument
            or DocumentKind.Spreadsheet
            or DocumentKind.Presentation;

    private static DocumentTypeDetectionResult Combine(DetectionContext context)
    {
        // Aggregate per-kind: keep the strongest signal, add a small bonus for agreement.
        var byKind = new Dictionary<DocumentKind, (int Best, int Count)>();
        foreach (var candidate in context.Candidates)
        {
            if (byKind.TryGetValue(candidate.Kind, out var current))
            {
                byKind[candidate.Kind] = (Math.Max(current.Best, candidate.Confidence), current.Count + 1);
            }
            else
            {
                byKind[candidate.Kind] = (candidate.Confidence, 1);
            }
        }

        // Deterministic winner: highest combined score, then strongest single signal, then a
        // fixed kind order — so identical input always resolves to the same kind.
        var ranked = byKind
            .Select(kv => (Kind: kv.Key, Score: Math.Min(100, kv.Value.Best + (kv.Value.Count - 1) * 5), kv.Value.Best))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Best)
            .ThenBy(x => x.Kind)
            .ToList();

        DocumentKind bestKind = ranked.Count > 0 ? ranked[0].Kind : DocumentKind.Unknown;
        int bestScore = ranked.Count > 0 ? ranked[0].Score : 0;

        var ordered = context.Candidates
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.Kind)
            .ToList();

        return new DocumentTypeDetectionResult
        {
            Kind = bestKind,
            Confidence = bestScore,
            Extension = context.Extension,
            ContentType = context.ContentType ?? FileFormatRegistry.ContentTypeForKind(bestKind),
            Candidates = ordered,
        };
    }
}
