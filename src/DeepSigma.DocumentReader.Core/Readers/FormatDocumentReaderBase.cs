using DeepSigma.DocumentReader.Core.Streams;

namespace DeepSigma.DocumentReader.Core.Readers;

/// <summary>
/// Base class for format readers. Handles the common <see cref="ReadAsync"/> skeleton —
/// size-limited buffering to a seekable stream and context construction — and delegates the
/// format-specific work to <see cref="ReadCoreAsync"/>.
/// </summary>
public abstract class FormatDocumentReaderBase : IFormatDocumentReader
{
    /// <inheritdoc />
    public abstract IReadOnlyCollection<DocumentKind> SupportedKinds { get; }

    /// <inheritdoc />
    /// <remarks>
    /// The default accepts any source; the composite reader narrows selection using
    /// <see cref="SupportedKinds"/> and <see cref="GetConfidence"/> against the detected kind.
    /// </remarks>
    public virtual bool CanRead(DocumentSource source) => true;

    /// <inheritdoc />
    public virtual int GetConfidence(DocumentSource source, DocumentTypeDetectionResult detectionResult)
    {
        ArgumentNullException.ThrowIfNull(detectionResult);
        return SupportedKinds.Contains(detectionResult.Kind) ? detectionResult.Confidence : 0;
    }

    /// <inheritdoc />
    public async Task<DocumentReadResult> ReadAsync(
        DocumentSource source,
        DocumentReadOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= DocumentReadOptions.Default;

        await using var buffered = await DocumentStreamBuffer
            .CreateAsync(source, options.MaxBytes, cancellationToken)
            .ConfigureAwait(false);

        var context = new DocumentReadContext(source, buffered, options);
        return await ReadCoreAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Performs the format-specific read against the prepared <paramref name="context"/>.</summary>
    protected abstract Task<DocumentReadResult> ReadCoreAsync(
        DocumentReadContext context,
        CancellationToken cancellationToken);
}
