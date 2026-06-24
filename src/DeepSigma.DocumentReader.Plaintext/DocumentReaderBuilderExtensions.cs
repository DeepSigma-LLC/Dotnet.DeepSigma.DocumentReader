using DeepSigma.DocumentReader.Plaintext;

namespace DeepSigma.DocumentReader;

/// <summary>Registration extensions for the lightweight text-family readers.</summary>
public static class PlaintextDocumentReaderBuilderExtensions
{
    /// <summary>Registers the plain-text reader.</summary>
    public static IDocumentReaderBuilder AddText(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<TextDocumentReader>();
    }

    /// <summary>Registers the JSON / JSON Lines reader.</summary>
    public static IDocumentReaderBuilder AddJson(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<JsonDocumentReader>();
    }

    /// <summary>Registers the CSV reader.</summary>
    public static IDocumentReaderBuilder AddCsv(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<CsvDocumentReader>();
    }

    /// <summary>Registers the Markdown reader.</summary>
    public static IDocumentReaderBuilder AddMarkdown(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<MarkdownDocumentReader>();
    }
}
