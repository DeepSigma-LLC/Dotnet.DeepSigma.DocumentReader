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

        return Combine(context);
    }

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

        DocumentKind bestKind = DocumentKind.Unknown;
        int bestScore = -1;
        foreach (var (kind, agg) in byKind)
        {
            int score = Math.Min(100, agg.Best + (agg.Count - 1) * 5);
            if (score > bestScore)
            {
                bestScore = score;
                bestKind = kind;
            }
        }

        var ordered = context.Candidates
            .OrderByDescending(c => c.Confidence)
            .ToList();

        return new DocumentTypeDetectionResult
        {
            Kind = bestKind,
            Confidence = bestScore < 0 ? 0 : bestScore,
            Extension = context.Extension,
            ContentType = context.ContentType ?? FileFormatRegistry.ContentTypeForKind(bestKind),
            Candidates = ordered,
        };
    }
}
