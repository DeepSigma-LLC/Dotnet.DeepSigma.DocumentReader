using System.Buffers;

namespace DeepSigma.DocumentReader.Core.Streams;

/// <summary>
/// Produces a seekable <see cref="BufferedDocument"/> from a (possibly non-seekable)
/// <see cref="DocumentSource"/>, enforcing a maximum size while it copies.
/// </summary>
public static class DocumentStreamBuffer
{
    /// <summary>Buffered content larger than this many bytes spills from memory to a temp file.</summary>
    public const int SpillToDiskThresholdBytes = 64 * 1024 * 1024;

    private const int CopyBufferSize = 81920;

    /// <summary>
    /// Returns a seekable view over <paramref name="source"/>. When the source stream is
    /// already seekable it is used directly (and not owned); otherwise its content is copied
    /// into memory, spilling to a delete-on-close temp file past
    /// <see cref="SpillToDiskThresholdBytes"/>.
    /// </summary>
    /// <exception cref="InvalidDocumentSourceException">The source stream is missing or unreadable.</exception>
    /// <exception cref="DocumentSizeLimitExceededException">The source exceeds <paramref name="maxBytes"/>.</exception>
    public static async Task<BufferedDocument> CreateAsync(
        DocumentSource source,
        long? maxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var input = source.Stream;
        if (input is null)
        {
            throw new InvalidDocumentSourceException("The document source has no stream.");
        }

        if (!input.CanRead)
        {
            throw new InvalidDocumentSourceException("The document source stream is not readable.");
        }

        if (input.CanSeek)
        {
            long length = input.Length;
            if (maxBytes is { } seekLimit && length > seekLimit)
            {
                throw new DocumentSizeLimitExceededException(seekLimit);
            }

            if (input.Position != 0)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            return new BufferedDocument(input, length, ownsStream: false);
        }

        return await BufferNonSeekableAsync(input, maxBytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<BufferedDocument> BufferNonSeekableAsync(
        Stream input,
        long? maxBytes,
        CancellationToken cancellationToken)
    {
        Stream target = new MemoryStream();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        long total = 0;
        bool spilled = false;
        try
        {
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, CopyBufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (maxBytes is { } limit && total > limit)
                {
                    throw new DocumentSizeLimitExceededException(limit);
                }

                if (!spilled && total > SpillToDiskThresholdBytes)
                {
                    target = await SpillToTempFileAsync((MemoryStream)target, cancellationToken).ConfigureAwait(false);
                    spilled = true;
                }

                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            await target.FlushAsync(cancellationToken).ConfigureAwait(false);
            target.Seek(0, SeekOrigin.Begin);
            return new BufferedDocument(target, total, ownsStream: true);
        }
        catch
        {
            await target.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<Stream> SpillToTempFileAsync(MemoryStream memory, CancellationToken cancellationToken)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "dsread-" + Path.GetRandomFileName());
        var file = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            CopyBufferSize,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            memory.Seek(0, SeekOrigin.Begin);
            await memory.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            await memory.DisposeAsync().ConfigureAwait(false);
            return file;
        }
        catch
        {
            await file.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
