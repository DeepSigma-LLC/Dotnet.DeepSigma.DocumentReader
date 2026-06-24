namespace DeepSigma.DocumentReader;

/// <summary>
/// Identifies where an extracted structure originated within its source document.
/// All members are optional; populate only those that apply to a given format.
/// </summary>
/// <param name="PageNumber">1-based page number (PDF, Word).</param>
/// <param name="SlideNumber">1-based slide number (PowerPoint).</param>
/// <param name="SheetIndex">0-based worksheet index (spreadsheets).</param>
/// <param name="SheetName">Worksheet name (spreadsheets).</param>
/// <param name="Row">0-based row index within a table or sheet.</param>
/// <param name="Column">0-based column index within a table or sheet.</param>
/// <param name="SectionPath">Slash-delimited heading path, e.g. <c>/Overview/Architecture</c>.</param>
public sealed record DocumentLocation(
    int? PageNumber = null,
    int? SlideNumber = null,
    int? SheetIndex = null,
    string? SheetName = null,
    int? Row = null,
    int? Column = null,
    string? SectionPath = null);
