using DeepSigma.DocumentReader.Office;

namespace DeepSigma.DocumentReader;

/// <summary>Registration extensions for the Office (DOCX/XLSX/PPTX) readers.</summary>
public static class OfficeDocumentReaderBuilderExtensions
{
    /// <summary>Registers the Word, Excel, and PowerPoint readers.</summary>
    public static IDocumentReaderBuilder AddOffice(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .AddReader<WordDocumentReader>()
            .AddReader<ExcelDocumentReader>()
            .AddReader<PowerPointDocumentReader>();
    }

    /// <summary>Registers only the Word reader.</summary>
    public static IDocumentReaderBuilder AddWord(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<WordDocumentReader>();
    }

    /// <summary>Registers only the Excel reader.</summary>
    public static IDocumentReaderBuilder AddExcel(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<ExcelDocumentReader>();
    }

    /// <summary>Registers only the PowerPoint reader.</summary>
    public static IDocumentReaderBuilder AddPowerPoint(this IDocumentReaderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddReader<PowerPointDocumentReader>();
    }
}
