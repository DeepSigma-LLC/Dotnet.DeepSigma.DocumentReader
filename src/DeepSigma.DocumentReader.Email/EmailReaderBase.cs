using System.Net;
using System.Text.RegularExpressions;
using DeepSigma.DocumentReader.Core.Readers;

namespace DeepSigma.DocumentReader.Email;

/// <summary>
/// Shared base for email readers. Holds the optional HTML-to-text conversion (reusing a
/// registered <see cref="IHtmlTextExtractor"/> when present) and assembles the common result
/// from an extracted <see cref="EmailDocumentFeature"/>, so the MIME (.eml) and Outlook (.msg)
/// readers differ only in how they parse.
/// </summary>
public abstract partial class EmailReaderBase : FormatDocumentReaderBase
{
    private readonly IHtmlTextExtractor? _htmlTextExtractor;

    /// <summary>Initializes the base, reusing the first registered <see cref="IHtmlTextExtractor"/> if any.</summary>
    protected EmailReaderBase(IEnumerable<IHtmlTextExtractor> htmlTextExtractors)
    {
        ArgumentNullException.ThrowIfNull(htmlTextExtractors);
        _htmlTextExtractor = htmlTextExtractors.FirstOrDefault();
    }

    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Email];

    /// <summary>Converts an HTML body to text via the registered extractor, or a naive tag strip.</summary>
    protected string? HtmlToText(string? html)
        => html is null ? null : _htmlTextExtractor is { } extractor ? extractor.ExtractText(html) : NaiveStrip(html);

    /// <summary>
    /// Chooses the result text from the available bodies, honoring
    /// <see cref="EmailReadOptions.PreferHtmlBody"/>.
    /// </summary>
    protected static string? ChooseBody(EmailReadOptions options, string? textBody, string? htmlAsText)
        => options.PreferHtmlBody && htmlAsText is not null ? htmlAsText : textBody ?? htmlAsText;

    /// <summary>Assembles the standard email result from the extracted feature, body, and attachments.</summary>
    protected DocumentReadResult BuildResult(
        DocumentReadContext context,
        EmailDocumentFeature feature,
        string? body,
        IReadOnlyList<DocumentAttachment> attachments)
        => new()
        {
            Source = context.CreateSourceInfo(DocumentKind.Email),
            Kind = DocumentKind.Email,
            Text = context.Options.ExtractText ? body : null,
            Attachments = attachments,
            Metadata = new DocumentMetadata
            {
                Title = string.IsNullOrEmpty(feature.Subject) ? null : feature.Subject,
                Author = feature.From.FirstOrDefault()?.Address,
                CreatedUtc = feature.Date,
            },
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [feature],
        };

    private static string NaiveStrip(string html)
    {
        string withoutTags = HtmlTagRegex().Replace(html, " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
