using System.Text;

namespace DeepSigma.DocumentReader.Core.Text;

/// <summary>
/// Shared helpers for reading a stream into bytes/text and decoding it in a byte-order-mark
/// aware way. Centralizes logic that would otherwise be duplicated across readers and the
/// type detector.
/// </summary>
public static class TextContent
{
    /// <summary>Reads the entire stream into a byte array, fast-pathing exposed memory streams.</summary>
    public static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
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

    /// <summary>Reads the entire stream and decodes it as text, honoring a leading BOM.</summary>
    public static async Task<string> ReadAllTextAsync(Stream stream, CancellationToken cancellationToken = default)
        => DecodeBomAware(await ReadAllBytesAsync(stream, cancellationToken).ConfigureAwait(false));

    /// <summary>Returns whether the bytes start with a UTF-8 or UTF-16 byte-order mark.</summary>
    public static bool HasBom(ReadOnlySpan<byte> bytes) =>
        (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) ||
        (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) ||
        (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF);

    /// <summary>
    /// Decodes bytes as text, honoring a leading UTF-8/UTF-16 BOM and otherwise assuming UTF-8.
    /// </summary>
    public static string DecodeBomAware(ReadOnlySpan<byte> bytes)
    {
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

    /// <summary>Removes <paramref name="encoding"/>'s preamble (BOM) from the start of the bytes, if present.</summary>
    public static byte[] StripBom(byte[] bytes, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(encoding);
        ReadOnlySpan<byte> preamble = encoding.GetPreamble();
        if (preamble.Length > 0 && bytes.Length >= preamble.Length && bytes.AsSpan(0, preamble.Length).SequenceEqual(preamble))
        {
            return bytes[preamble.Length..];
        }

        return bytes;
    }

    /// <summary>Normalizes CRLF and lone CR line endings to LF.</summary>
    public static string NormalizeLineEndings(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}
