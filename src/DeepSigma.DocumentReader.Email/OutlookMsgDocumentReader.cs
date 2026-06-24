using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Email.Internal;
using MsgReader.Outlook;

namespace DeepSigma.DocumentReader.Email;

/// <summary>
/// Reads Outlook email messages (<c>.msg</c>, an OLE2 compound file) using MsgReader:
/// sender/recipients, subject, sent date, text/HTML body, and attachments. Produces the same
/// <see cref="DocumentKind.Email"/> result as the MIME (<c>.eml</c>) reader.
/// </summary>
public sealed class OutlookMsgDocumentReader : EmailReaderBase
{
    /// <summary>Creates a reader that uses a naive HTML-to-text fallback.</summary>
    public OutlookMsgDocumentReader() : this([]) { }

    /// <summary>Creates a reader, reusing the first registered <see cref="IHtmlTextExtractor"/> if any.</summary>
    public OutlookMsgDocumentReader(IEnumerable<IHtmlTextExtractor> htmlTextExtractors) : base(htmlTextExtractors) { }

    /// <inheritdoc />
    /// <remarks>Claims OLE2 content (and the <c>.msg</c> extension); plain MIME goes to the .eml reader.</remarks>
    public override int GetConfidence(DocumentSource source, DocumentTypeDetectionResult detectionResult)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(detectionResult);
        if (OleSignature.IsOle2(source.Stream))
        {
            return 95;
        }

        return string.Equals(detectionResult.Extension, ".msg", StringComparison.OrdinalIgnoreCase) ? 90 : 0;
    }

    /// <inheritdoc />
    protected override Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<EmailReadOptions>();
        using var message = new Storage.Message(context.Stream);

        string? textBody = NullIfEmpty(message.BodyText);
        string? htmlBody = NullIfEmpty(message.BodyHtml);
        string? body = ChooseBody(options, textBody, HtmlToText(htmlBody));

        var feature = new EmailDocumentFeature
        {
            Subject = message.Subject,
            From = MapSender(message),
            To = MapRecipients(message, RecipientType.To),
            Cc = MapRecipients(message, RecipientType.Cc),
            Date = message.SentOn,
            TextBody = textBody,
            HtmlBody = htmlBody,
        };

        var attachments = context.Options.ExtractAttachments ? ReadAttachments(message) : [];
        return Task.FromResult(BuildResult(context, feature, body, attachments));
    }

    private static IReadOnlyList<EmailAddress> MapSender(Storage.Message message)
        => message.Sender is { Email: { Length: > 0 } email } sender
            ? [new EmailAddress(NullIfEmpty(sender.DisplayName), email)]
            : [];

    private static IReadOnlyList<EmailAddress> MapRecipients(Storage.Message message, RecipientType type)
        => message.Recipients
            .Where(r => r.Type == type && !string.IsNullOrEmpty(r.Email))
            .Select(r => new EmailAddress(NullIfEmpty(r.DisplayName), r.Email!))
            .ToArray();

    private static IReadOnlyList<DocumentAttachment> ReadAttachments(Storage.Message message)
    {
        var attachments = new List<DocumentAttachment>();
        foreach (object item in message.Attachments)
        {
            if (item is Storage.Attachment attachment && attachment.Data is { } data)
            {
                attachments.Add(new ByteArrayDocumentAttachment(data) { FileName = attachment.FileName });
            }
        }

        return attachments;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
