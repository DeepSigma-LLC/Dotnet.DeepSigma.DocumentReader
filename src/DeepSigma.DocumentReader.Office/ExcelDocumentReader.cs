using System.Text;
using ClosedXML.Excel;
using DeepSigma.DocumentReader.Core.Readers;

namespace DeepSigma.DocumentReader.Office;

/// <summary>
/// Reads Excel (XLSX) workbooks using ClosedXML. Each worksheet's used range becomes a
/// <see cref="DocumentTable"/> with typed cell values and cell addresses. Formulas are not
/// recalculated; cached values are used and a warning is raised when formulas are present.
/// </summary>
public sealed class ExcelDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Spreadsheet];

    /// <inheritdoc />
    protected override Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<ExcelReadOptions>();

        using var workbook = new XLWorkbook(context.Stream);

        var text = new StringBuilder();
        var sheets = new List<SpreadsheetSheet>();
        var tables = new List<DocumentTable>();
        bool anyFormula = false;

        int sheetIndex = 0;
        foreach (IXLWorksheet worksheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!options.IncludeHiddenSheets && worksheet.Visibility != XLWorksheetVisibility.Visible)
            {
                sheetIndex++;
                continue;
            }

            DocumentTable table = ReadSheet(worksheet, sheetIndex, options, context, ref anyFormula);
            tables.Add(table);
            sheets.Add(new SpreadsheetSheet { SheetIndex = sheetIndex, Name = worksheet.Name, Table = table });

            AppendSheetText(text, worksheet.Name, table);
            sheetIndex++;
        }

        if (anyFormula)
        {
            context.AddWarning(WarningCodes.ExcelFormulaNotCalculated,
                "Formulas were not recalculated; cached cell values were used.");
        }

        var result = new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Spreadsheet),
            Kind = DocumentKind.Spreadsheet,
            Text = context.Options.ExtractText ? text.ToString().TrimEnd('\n') : null,
            Tables = context.Options.ExtractTables ? tables : [],
            Metadata = ReadMetadata(workbook),
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [new SpreadsheetDocumentFeature { Sheets = sheets }],
        };

        return Task.FromResult(result);
    }

    private static DocumentTable ReadSheet(
        IXLWorksheet worksheet,
        int sheetIndex,
        ExcelReadOptions options,
        DocumentReadContext context,
        ref bool anyFormula)
    {
        var rows = new List<DocumentTableRow>();
        IXLRange? used = worksheet.RangeUsed();
        if (used is not null)
        {
            int rowCount = used.RowCount();
            int columnCount = used.ColumnCount();
            int maxRows = options.MaxRowsPerSheet ?? int.MaxValue;

            for (int r = 0; r < rowCount; r++)
            {
                if (r >= maxRows)
                {
                    context.AddWarning(WarningCodes.ExcelMaxRowsExceeded,
                        $"Worksheet '{worksheet.Name}' was truncated to {maxRows} rows.",
                        new DocumentLocation(SheetIndex: sheetIndex, SheetName: worksheet.Name));
                    break;
                }

                IXLRangeRow row = used.Row(r + 1);
                var cells = new DocumentTableCell[columnCount];
                for (int c = 0; c < columnCount; c++)
                {
                    IXLCell cell = row.Cell(c + 1);
                    if (cell.HasFormula)
                    {
                        anyFormula = true;
                    }

                    cells[c] = new DocumentTableCell
                    {
                        RowIndex = r,
                        ColumnIndex = c,
                        Text = cell.GetFormattedString(),
                        RawValue = ToRawValue(cell.Value),
                        Location = new DocumentLocation(
                            SheetIndex: sheetIndex,
                            SheetName: worksheet.Name,
                            Row: cell.Address.RowNumber,
                            Column: cell.Address.ColumnNumber),
                    };
                }

                rows.Add(new DocumentTableRow { RowIndex = r, Cells = cells });
            }
        }

        return new DocumentTable
        {
            Name = worksheet.Name,
            Location = new DocumentLocation(SheetIndex: sheetIndex, SheetName: worksheet.Name),
            Rows = rows,
        };
    }

    private static object? ToRawValue(XLCellValue value) => value.Type switch
    {
        XLDataType.Number => value.GetNumber(),
        XLDataType.Text => value.GetText(),
        XLDataType.Boolean => value.GetBoolean(),
        XLDataType.DateTime => value.GetDateTime(),
        XLDataType.TimeSpan => value.GetTimeSpan(),
        // Surface the error code (e.g. "#REF!") so an error cell is distinguishable from blank.
        XLDataType.Error => value.GetError().ToString(),
        _ => null,
    };

    private static void AppendSheetText(StringBuilder text, string sheetName, DocumentTable table)
    {
        text.Append("# ").Append(sheetName).Append('\n');
        foreach (DocumentTableRow row in table.Rows)
        {
            text.AppendJoin('\t', row.Cells.Select(c => c.Text ?? string.Empty)).Append('\n');
        }

        text.Append('\n');
    }

    private static DocumentMetadata ReadMetadata(XLWorkbook workbook)
    {
        var properties = workbook.Properties;
        return new DocumentMetadata
        {
            Title = string.IsNullOrEmpty(properties.Title) ? null : properties.Title,
            Author = string.IsNullOrEmpty(properties.Author) ? null : properties.Author,
            CreatedUtc = properties.Created == default ? null : OfficeMetadata.ToOffset(properties.Created),
            ModifiedUtc = properties.Modified == default ? null : OfficeMetadata.ToOffset(properties.Modified),
        };
    }
}
