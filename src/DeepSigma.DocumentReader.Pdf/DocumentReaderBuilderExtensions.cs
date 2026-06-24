using DeepSigma.DocumentReader.Pdf;

namespace DeepSigma.DocumentReader;

/// <summary>Registration extension for the PDF reader.</summary>
public static class PdfDocumentReaderBuilderExtensions
{
    /// <summary>Registers the PDF reader.</summary>
    public static IDocumentReaderBuilder AddPdf(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<PdfDocumentReader>();
    }
}
