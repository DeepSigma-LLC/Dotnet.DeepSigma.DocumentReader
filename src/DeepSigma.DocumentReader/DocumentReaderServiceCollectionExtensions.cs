using DeepSigma.DocumentReader.Export;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.DocumentReader;

/// <summary>Convenience registration that wires the full default reader and exporter set.</summary>
public static class DocumentReaderServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core reader, the default readers (text, Markdown, JSON, CSV, the Office
    /// formats DOCX/XLSX/PPTX, PDF, HTML, and email), and the text/Markdown/JSON exporters
    /// with their resolver. Does not include OCR.
    /// </summary>
    public static IServiceCollection AddDeepSigmaDocumentReaderDefaults(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDeepSigmaDocumentReader()
            .AddText()
            .AddMarkdown()
            .AddJson()
            .AddCsv()
            .AddOffice()
            .AddPdf()
            .AddHtml()
            .AddEmail();

        services.AddSingleton<IDocumentResultExporter, TextResultExporter>();
        services.AddSingleton<IDocumentResultExporter, MarkdownResultExporter>();
        services.AddSingleton<IDocumentResultExporter, JsonResultExporter>();
        services.AddSingleton<IExporterResolver, ExporterResolver>();

        return services;
    }
}
