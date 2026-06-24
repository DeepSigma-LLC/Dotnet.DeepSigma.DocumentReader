namespace DeepSigma.DocumentReader;

/// <summary>Options for the PDF reader.</summary>
public sealed class PdfReadOptions : IFormatReadOptions
{
    /// <summary>
    /// Maximum number of pages to read. When <see langword="null"/>, falls back to
    /// <see cref="DocumentReadOptions.MaxPages"/>.
    /// </summary>
    public int? MaxPages { get; init; }

    /// <summary>
    /// Whether to preserve the visual layout (coordinate-ordered) rather than content
    /// (reading) order when extracting page text. Default <see langword="false"/>.
    /// </summary>
    public bool PreserveLayout { get; init; }

    /// <summary>
    /// Whether to attempt best-effort table extraction by clustering word coordinates.
    /// Default <see langword="false"/>; results are approximate and carry a confidence score.
    /// </summary>
    public bool ExtractTables { get; init; }

    /// <summary>
    /// Tables with a confidence at or below this threshold raise a low-confidence warning.
    /// Default 0.6.
    /// </summary>
    public double LowConfidenceThreshold { get; init; } = 0.6;
}
