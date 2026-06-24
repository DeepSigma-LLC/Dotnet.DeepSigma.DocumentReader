using DeepSigma.DocumentReader.Email;

namespace DeepSigma.DocumentReader;

/// <summary>Registration extension for the email reader.</summary>
public static class EmailDocumentReaderBuilderExtensions
{
    /// <summary>
    /// Registers the email readers for both MIME (<c>.eml</c>) and Outlook (<c>.msg</c>)
    /// messages. If an <see cref="IHtmlTextExtractor"/> is also registered (via <c>AddHtml()</c>),
    /// it is used to convert HTML bodies to text.
    /// </summary>
    public static IDocumentReaderBuilder AddEmail(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .AddReader<EmailDocumentReader>()
            .AddReader<OutlookMsgDocumentReader>();
    }
}
