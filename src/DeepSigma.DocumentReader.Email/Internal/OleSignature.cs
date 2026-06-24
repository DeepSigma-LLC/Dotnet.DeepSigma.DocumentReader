namespace DeepSigma.DocumentReader.Email.Internal;

/// <summary>Detects the OLE2 / Compound File Binary signature used by Outlook <c>.msg</c> files.</summary>
internal static class OleSignature
{
    private static readonly byte[] Magic = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    /// Returns whether the stream begins with the OLE2 signature. For a seekable stream the
    /// original position is restored; a non-seekable stream is assumed not to be OLE2.
    /// </summary>
    public static bool IsOle2(Stream stream)
    {
        if (stream is null || !stream.CanSeek)
        {
            return false;
        }

        long position = stream.Position;
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            Span<byte> header = stackalloc byte[8];
            int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
            return read == header.Length && header.SequenceEqual(Magic);
        }
        finally
        {
            stream.Seek(position, SeekOrigin.Begin);
        }
    }
}
