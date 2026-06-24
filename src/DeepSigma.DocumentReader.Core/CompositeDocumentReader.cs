using DeepSigma.DocumentReader.Core.Streams;

namespace DeepSigma.DocumentReader.Core;

/// <summary>
/// The default <see cref="IDocumentReader"/>. Buffers the source once, detects its type,
/// selects the highest-confidence registered reader for that kind, and applies the
/// cross-cutting size and timeout limits so individual format readers stay simple.
/// </summary>
public sealed class CompositeDocumentReader : IDocumentReader
{
    private readonly IReadOnlyList<IFormatDocumentReader> _readers;
    private readonly IDocumentTypeDetector _detector;

    /// <summary>Creates a composite reader over the given format readers and detector.</summary>
    public CompositeDocumentReader(IEnumerable<IFormatDocumentReader> readers, IDocumentTypeDetector detector)
    {
        ArgumentNullException.ThrowIfNull(readers);
        ArgumentNullException.ThrowIfNull(detector);
        _readers = readers.ToList();
        _detector = detector;
    }

    /// <inheritdoc />
    public bool CanRead(DocumentSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _readers.Any(r => r.CanRead(source));
    }

    /// <inheritdoc />
    public async Task<DocumentReadResult> ReadAsync(
        DocumentSource source,
        DocumentReadOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= DocumentReadOptions.Default;

        // Buffer once up front so detection and the selected reader share a seekable stream
        // and the size limit is enforced a single time.
        await using var buffered = await DocumentStreamBuffer
            .CreateAsync(source, options.MaxBytes, cancellationToken)
            .ConfigureAwait(false);

        var bufferedSource = DocumentSource.FromStream(buffered.Stream, source.FileName, source.ContentType);

        var detection = await _detector.DetectAsync(bufferedSource, cancellationToken).ConfigureAwait(false);
        buffered.Rewind();

        var reader = SelectReader(bufferedSource, detection);
        if (reader is null)
        {
            throw new UnsupportedDocumentTypeException(source.FileName, detection.Kind);
        }

        if (options.Timeout is { } timeout)
        {
            return await ReadWithTimeoutAsync(reader, bufferedSource, options, detection, timeout, cancellationToken)
                .ConfigureAwait(false);
        }

        return await reader.ReadAsync(bufferedSource, options, cancellationToken).ConfigureAwait(false);
    }

    private IFormatDocumentReader? SelectReader(DocumentSource source, DocumentTypeDetectionResult detection)
        => _readers
            .Where(r => r.CanRead(source) && r.SupportedKinds.Contains(detection.Kind))
            .Select(r => (Reader: r, Confidence: r.GetConfidence(source, detection)))
            .Where(x => x.Confidence > 0)
            .OrderByDescending(x => x.Confidence)
            .Select(x => x.Reader)
            .FirstOrDefault();

    private static async Task<DocumentReadResult> ReadWithTimeoutAsync(
        IFormatDocumentReader reader,
        DocumentSource source,
        DocumentReadOptions options,
        DocumentTypeDetectionResult detection,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            return await reader.ReadAsync(source, options, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // The timeout fired (not the caller's token) and no usable result was produced.
            throw new DocumentTimeoutException(timeout);
        }
    }
}
