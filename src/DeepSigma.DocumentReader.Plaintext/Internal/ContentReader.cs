using System.Text;

namespace DeepSigma.DocumentReader.Plaintext.Internal;

/// <summary>Shared helpers for reading a seekable content stream into bytes/text.</summary>
internal static class ContentReader
{
    /// <summary>Reads the entire stream into a byte array.</summary>
    public static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> segment))
        {
            var copy = new byte[segment.Count];
            Array.Copy(segment.Array!, segment.Offset, copy, 0, segment.Count);
            return copy;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Decodes bytes as UTF-8, honoring a leading UTF-8/UTF-16 byte-order mark.</summary>
    public static string DecodeUtf8(byte[] bytes)
    {
        ReadOnlySpan<byte> span = bytes;
        if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(span[3..]);
        }

        if (span.Length >= 2 && span[0] == 0xFF && span[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(span[2..]);
        }

        if (span.Length >= 2 && span[0] == 0xFE && span[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(span[2..]);
        }

        return Encoding.UTF8.GetString(span);
    }

    /// <summary>Normalizes CRLF and lone CR line endings to LF.</summary>
    public static string NormalizeLineEndings(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
