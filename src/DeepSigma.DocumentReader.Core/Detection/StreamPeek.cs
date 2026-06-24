namespace DeepSigma.DocumentReader.Core.Detection;

/// <summary>Reads a prefix of a stream, restoring the position afterwards when possible.</summary>
public static class StreamPeek
{
    /// <summary>
    /// Reads up to <paramref name="maxBytes"/> bytes from the start of <paramref name="stream"/>.
    /// If the stream is seekable, its original position is restored before returning.
    /// </summary>
    public static async Task<byte[]> ReadPrefixAsync(Stream stream, int maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        long originalPosition = stream.CanSeek ? stream.Position : 0;
        if (stream.CanSeek && originalPosition != 0)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var buffer = new byte[maxBytes];
        int total = 0;
        try
        {
            int read;
            while (total < maxBytes &&
                   (read = await stream.ReadAsync(buffer.AsMemory(total, maxBytes - total), cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
            }
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
        }

        if (total == maxBytes)
        {
            return buffer;
        }

        var trimmed = new byte[total];
        Array.Copy(buffer, trimmed, total);
        return trimmed;
    }
}
