namespace DeepSigma.DocumentReader;

/// <summary>Options for the Excel (XLSX) reader.</summary>
public sealed class ExcelReadOptions : IFormatReadOptions
{
    /// <summary>Maximum number of used rows to read per worksheet. Default 1,000,000.</summary>
    public int? MaxRowsPerSheet { get; init; } = 1_000_000;

    /// <summary>Whether to read hidden worksheets. Default <see langword="false"/>.</summary>
    public bool IncludeHiddenSheets { get; init; }
}

/// <summary>A single worksheet within a spreadsheet.</summary>
public sealed class SpreadsheetSheet
{
    /// <summary>The 0-based worksheet index.</summary>
    public required int SheetIndex { get; init; }

    /// <summary>The worksheet name.</summary>
    public required string Name { get; init; }

    /// <summary>The worksheet's used range as a table.</summary>
    public required DocumentTable Table { get; init; }
}

/// <summary>Format-specific spreadsheet details attached to a read result.</summary>
public sealed class SpreadsheetDocumentFeature : IDocumentFeature
{
    /// <inheritdoc />
    public string Name => "Spreadsheet";

    /// <summary>The worksheets in workbook order.</summary>
    public IReadOnlyList<SpreadsheetSheet> Sheets { get; init; } = [];
}
