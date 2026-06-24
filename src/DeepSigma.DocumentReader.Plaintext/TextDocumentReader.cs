using System.Text;
using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Plaintext.Internal;

namespace DeepSigma.DocumentReader.Plaintext;

/// <summary>
/// Reads plain-text documents. Handles byte-order marks, falls back from strict UTF-8 to
/// Latin-1 on invalid byte sequences (with a warning), and optionally normalizes line
/// endings and truncates to a character budget. Also serves as the universal fallback for
/// otherwise-unrecognized text.
/// </summary>
public sealed class TextDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.PlainText];

    /// <inheritdoc />
    protected override async Task<DocumentReadResult> ReadCoreAsync(
        DocumentReadContext context,
        CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<TextReadOptions>();
        byte[] bytes = await ContentReader.ReadAllBytesAsync(context.Stream, cancellationToken).ConfigureAwait(false);

        string text = Decode(bytes, options, context);

        if (options.NormalizeLineEndings)
        {
            text = ContentReader.NormalizeLineEndings(text);
        }

        if (options.MaxCharacters is { } max && text.Length > max)
        {
            text = text[..(int)Math.Min(max, int.MaxValue)];
            context.AddWarning(WarningCodes.TextTruncated,
                $"Text was truncated to the configured maximum of {max} characters.");
        }

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.PlainText),
            Kind = DocumentKind.PlainText,
            Text = context.Options.ExtractText ? text : null,
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
        };
    }

    private static string Decode(byte[] bytes, TextReadOptions options, DocumentReadContext context)
    {
        if (options.Encoding is { } explicitEncoding)
        {
            return explicitEncoding.GetString(StripBom(bytes, explicitEncoding));
        }

        // BOM-based detection covers UTF-8/UTF-16; ContentReader handles those.
        if (HasBom(bytes) || !options.DetectEncoding)
        {
            return ContentReader.DecodeUtf8(bytes);
        }

        // No BOM: try strict UTF-8, falling back to Latin-1 (1:1 byte mapping) on invalid bytes.
        try
        {
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return strict.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            context.AddWarning(WarningCodes.UnsupportedEncoding,
                "Content was not valid UTF-8; decoded as Latin-1 (ISO-8859-1).");
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static bool HasBom(byte[] b) =>
        (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) ||
        (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) ||
        (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF);

    private static byte[] StripBom(byte[] bytes, Encoding encoding)
    {
        ReadOnlySpan<byte> preamble = encoding.GetPreamble();
        if (preamble.Length > 0 && bytes.Length >= preamble.Length && bytes.AsSpan(0, preamble.Length).SequenceEqual(preamble))
        {
            return bytes[preamble.Length..];
        }

        return bytes;
    }
}
