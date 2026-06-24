namespace DeepSigma.DocumentReader.Core.Streams;

/// <summary>
/// A seekable view over a document's content, produced by <see cref="DocumentStreamBuffer"/>.
/// Owns any temporary buffer it allocated (a memory stream or a delete-on-close temp file)
/// and releases it on disposal; never disposes a caller-owned seekable stream.
/// </summary>
public sealed class BufferedDocument : IAsyncDisposable, IDisposable
{
    private readonly bool _ownsStream;

    internal BufferedDocument(Stream stream, long length, bool ownsStream)
    {
        Stream = stream;
        Length = length;
        _ownsStream = ownsStream;
    }

    /// <summary>A readable, seekable stream positioned at the start of the content.</summary>
    public Stream Stream { get; }

    /// <summary>The total length of the content in bytes.</summary>
    public long Length { get; }

    /// <summary>Resets the stream position to the beginning.</summary>
    public void Rewind() => Stream.Seek(0, SeekOrigin.Begin);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsStream)
        {
            Stream.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_ownsStream)
        {
            await Stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
