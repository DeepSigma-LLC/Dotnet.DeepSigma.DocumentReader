namespace DeepSigma.DocumentReader;

/// <summary>
/// Stable, machine-readable codes used in <see cref="DocumentWarning.Code"/>. Codes are
/// namespaced by area; the list grows as readers are added.
/// </summary>
public static class WarningCodes
{
    // Cross-cutting document codes.

    /// <summary>The source exceeded the configured size limit.</summary>
    public const string SizeLimitExceeded = "Document.SizeLimitExceeded";

    /// <summary>The read operation exceeded its timeout but produced a partial result.</summary>
    public const string TimeoutExceeded = "Document.TimeoutExceeded";

    /// <summary>The text could not be decoded with full fidelity.</summary>
    public const string UnsupportedEncoding = "Document.UnsupportedEncoding";

    /// <summary>The document is password-protected or encrypted.</summary>
    public const string PasswordProtected = "Document.PasswordProtected";

    // JSON.

    /// <summary>JSON nesting exceeded the configured maximum depth.</summary>
    public const string JsonMaxDepthExceeded = "Json.MaxDepthExceeded";

    /// <summary>The configured maximum number of JSON records was reached.</summary>
    public const string JsonMaxRecordsExceeded = "Json.MaxRecordsExceeded";

    /// <summary>A JSON Lines record could not be parsed and was skipped.</summary>
    public const string JsonMalformedRecord = "Json.MalformedRecord";

    // CSV.

    /// <summary>A CSV row was malformed and was skipped or recovered.</summary>
    public const string CsvMalformedRow = "Csv.MalformedRow";

    /// <summary>The configured maximum number of CSV rows was reached.</summary>
    public const string CsvMaxRowsExceeded = "Csv.MaxRowsExceeded";

    // Text.

    /// <summary>The configured maximum character count was reached and content was truncated.</summary>
    public const string TextTruncated = "Text.Truncated";

    // Office.

    /// <summary>Spreadsheet formulas were not recalculated; cached values were used.</summary>
    public const string ExcelFormulaNotCalculated = "Excel.FormulaNotCalculated";

    /// <summary>The configured maximum number of spreadsheet rows was reached.</summary>
    public const string ExcelMaxRowsExceeded = "Excel.MaxRowsExceeded";

    /// <summary>A hidden slide was skipped.</summary>
    public const string PowerPointHiddenSlideSkipped = "PowerPoint.HiddenSlideSkipped";

    // PDF.

    /// <summary>A page has no extractable text layer (likely scanned); OCR would be required.</summary>
    public const string PdfTextLayerMissing = "Pdf.TextLayerMissing";

    /// <summary>The configured maximum number of pages was reached.</summary>
    public const string PdfMaxPagesExceeded = "Pdf.MaxPagesExceeded";

    /// <summary>A table was extracted from a PDF with low confidence in its structure.</summary>
    public const string PdfLowConfidenceTableExtraction = "Pdf.LowConfidenceTableExtraction";
}
