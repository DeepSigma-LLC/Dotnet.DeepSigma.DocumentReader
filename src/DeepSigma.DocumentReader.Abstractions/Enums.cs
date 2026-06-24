namespace DeepSigma.DocumentReader;

/// <summary>
/// The high-level kind of a document, independent of its concrete file extension.
/// </summary>
public enum DocumentKind
{
    /// <summary>The kind could not be determined.</summary>
    Unknown = 0,

    /// <summary>Plain, unstructured text.</summary>
    PlainText,

    /// <summary>Markdown (CommonMark / GitHub-flavored).</summary>
    Markdown,

    /// <summary>A single JSON document.</summary>
    Json,

    /// <summary>Newline-delimited JSON (JSON Lines / NDJSON).</summary>
    JsonLines,

    /// <summary>Comma- or otherwise-delimited tabular text.</summary>
    Csv,

    /// <summary>Portable Document Format.</summary>
    Pdf,

    /// <summary>A word-processing document (e.g. DOCX).</summary>
    WordDocument,

    /// <summary>A spreadsheet workbook (e.g. XLSX).</summary>
    Spreadsheet,

    /// <summary>A slide presentation (e.g. PPTX).</summary>
    Presentation,

    /// <summary>An email message (e.g. EML).</summary>
    Email,

    /// <summary>An HTML document.</summary>
    Html,

    /// <summary>A raster or vector image.</summary>
    Image,

    /// <summary>An archive container (e.g. ZIP).</summary>
    Archive,
}

/// <summary>
/// A coarse, comparable indication of how reliable an extraction is believed to be.
/// </summary>
public enum ExtractionQuality
{
    /// <summary>Quality was not assessed.</summary>
    Unknown = 0,

    /// <summary>Extraction is believed to be complete and accurate.</summary>
    High,

    /// <summary>Extraction is usable but some fidelity may be lost.</summary>
    Medium,

    /// <summary>Extraction succeeded only partially or with low confidence.</summary>
    Low,

    /// <summary>No useful content could be extracted.</summary>
    Failed,
}
