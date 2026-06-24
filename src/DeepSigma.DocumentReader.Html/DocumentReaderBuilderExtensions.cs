using DeepSigma.DocumentReader.Html;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeepSigma.DocumentReader;

/// <summary>Registration extension for the HTML reader.</summary>
public static class HtmlDocumentReaderBuilderExtensions
{
    /// <summary>
    /// Registers the HTML reader and an <see cref="IHtmlTextExtractor"/> that other readers
    /// (e.g. email) can reuse for HTML body conversion.
    /// </summary>
    public static IDocumentReaderBuilder AddHtml(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddSingleton<IHtmlTextExtractor, HtmlTextExtractor>();
        return builder.AddReader<HtmlDocumentReader>();
    }
}
