using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Email.Internal;
using MimeKit;

namespace DeepSigma.DocumentReader.Email;

/// <summary>
/// Reads MIME email messages (<c>.eml</c>) using MimeKit: headers, sender/recipients, subject,
/// date, the text body (or HTML body converted to text), and attachments. Remote content is
/// never fetched. Attachments are exposed for the caller to read recursively under its own
/// limits.
/// </summary>
public sealed class EmailDocumentReader : EmailReaderBase
{
    /// <summary>Creates a reader that uses a naive HTML-to-text fallback.</summary>
    public EmailDocumentReader() : this([]) { }

    /// <summary>Creates a reader, reusing the first registered <see cref="IHtmlTextExtractor"/> if any.</summary>
    public EmailDocumentReader(IEnumerable<IHtmlTextExtractor> htmlTextExtractors) : base(htmlTextExtractors) { }

    /// <inheritdoc />
    /// <remarks>Yields to the Outlook <c>.msg</c> reader for OLE2 content, which MimeKit cannot parse.</remarks>
    public override int GetConfidence(DocumentSource source, DocumentTypeDetectionResult detectionResult)
    {
        ArgumentNullException.ThrowIfNull(source);
        return OleSignature.IsOle2(source.Stream) ? 0 : base.GetConfidence(source, detectionResult);
    }

    /// <inheritdoc />
    protected override async Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<EmailReadOptions>();
        MimeMessage message = await MimeMessage.LoadAsync(context.Stream, cancellationToken).ConfigureAwait(false);

        string? textBody = message.TextBody;
        string? htmlBody = message.HtmlBody;
        string? body = ChooseBody(options, textBody, HtmlToText(htmlBody));

        var feature = new EmailDocumentFeature
        {
            Subject = message.Subject,
            From = MapAddresses(message.From),
            To = MapAddresses(message.To),
            Cc = MapAddresses(message.Cc),
            Date = message.Date == default ? null : message.Date,
            TextBody = textBody,
            HtmlBody = htmlBody,
            Headers = MapHeaders(message.Headers),
        };

        var attachments = context.Options.ExtractAttachments ? ReadAttachments(message) : [];
        return BuildResult(context, feature, body, attachments);
    }

    private static IReadOnlyList<EmailAddress> MapAddresses(InternetAddressList list)
        => list.Mailboxes.Select(m => new EmailAddress(string.IsNullOrEmpty(m.Name) ? null : m.Name, m.Address)).ToArray();

    private static IReadOnlyList<DocumentAttachment> ReadAttachments(MimeMessage message)
    {
        var attachments = new List<DocumentAttachment>();
        foreach (MimeEntity entity in message.Attachments)
        {
            using var buffer = new MemoryStream();
            switch (entity)
            {
                case MimePart part:
                    part.Content?.DecodeTo(buffer);
                    break;
                case MessagePart messagePart:
                    messagePart.Message?.WriteTo(buffer);
                    break;
                default:
                    entity.WriteTo(buffer);
                    break;
            }

            attachments.Add(new ByteArrayDocumentAttachment(buffer.ToArray())
            {
                FileName = entity.ContentDisposition?.FileName ?? (entity as MimePart)?.FileName,
                ContentType = entity.ContentType?.MimeType,
            });
        }

        return attachments;
    }

    private static IReadOnlyDictionary<string, string> MapHeaders(HeaderList headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Header header in headers)
        {
            // RFC 5322 permits repeated header fields (Received, DKIM-Signature, …); preserve
            // every occurrence by joining with newlines rather than overwriting.
            result[header.Field] = result.TryGetValue(header.Field, out string? existing)
                ? $"{existing}\n{header.Value}"
                : header.Value;
        }

        return result;
    }
}
