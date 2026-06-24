using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Core.Text;

namespace DeepSigma.DocumentReader.Plaintext;

/// <summary>
/// Reads delimited text (CSV/TSV) using CsvHelper. Detects the delimiter when not supplied,
/// projects rows into a single <see cref="DocumentTable"/>, and reports malformed rows as
/// warnings instead of failing the whole document.
/// </summary>
public sealed class CsvDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Csv];

    /// <inheritdoc />
    protected override async Task<DocumentReadResult> ReadCoreAsync(
        DocumentReadContext context,
        CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<CsvReadOptions>();
        byte[] bytes = await TextContent.ReadAllBytesAsync(context.Stream, cancellationToken).ConfigureAwait(false);
        string text = TextContent.DecodeBomAware(bytes);
        string delimiter = options.Delimiter ?? DetectDelimiter(text);

        int malformed = 0;
        var configuration = new CsvConfiguration(options.Culture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = options.HasHeaderRecord,
            DetectColumnCountChanges = false,
            BadDataFound = args =>
            {
                malformed++;
                int row = args.Context?.Parser?.Row ?? 0;
                context.AddWarning(WarningCodes.CsvMalformedRow,
                    $"Malformed CSV data at row {row}.",
                    new DocumentLocation(Row: row));
            },
            ReadingExceptionOccurred = args =>
            {
                malformed++;
                context.AddWarning(WarningCodes.CsvMalformedRow, "Skipped malformed CSV row.", exception: args.Exception);
                return false;
            },
        };

        var builder = new DocumentTableBuilder { Name = context.Source.FileName };

        using (var reader = new StringReader(text))
        using (var csv = new CsvReader(reader, configuration))
        {
            if (csv.Read())
            {
                if (options.HasHeaderRecord)
                {
                    csv.ReadHeader();
                    builder.Headers = csv.HeaderRecord ?? [];
                }
                else
                {
                    builder.AddRow(GetCurrentFields(csv));
                }

                while (csv.Read())
                {
                    if (options.MaxRows is { } maxRows && builder.RowCount >= maxRows)
                    {
                        context.AddWarning(WarningCodes.CsvMaxRowsExceeded,
                            $"Stopped reading after the configured maximum of {maxRows} rows.");
                        break;
                    }

                    builder.AddRow(GetCurrentFields(csv));
                }
            }
        }

        DocumentTable table = builder.Build();

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Csv),
            Kind = DocumentKind.Csv,
            Text = context.Options.ExtractText ? RenderText(table, delimiter) : null,
            Tables = context.Options.ExtractTables ? [table] : [],
            Quality = malformed > 0 ? ExtractionQuality.Medium : ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
        };
    }

    private static IReadOnlyList<string?> GetCurrentFields(CsvReader csv)
    {
        string[]? record = csv.Parser.Record;
        if (record is not null)
        {
            return record;
        }

        var fields = new string?[csv.Parser.Count];
        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = csv.GetField(i);
        }

        return fields;
    }

    private static string DetectDelimiter(string text)
    {
        List<string> lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(l => l.Length > 0)
            .Take(10)
            .ToList();

        if (lines.Count == 0)
        {
            return ",";
        }

        foreach (char delimiter in (ReadOnlySpan<char>)[',', '\t', ';', '|'])
        {
            int first = lines[0].Count(c => c == delimiter);
            if (first >= 1 && lines.All(l => l.Count(c => c == delimiter) == first))
            {
                return delimiter.ToString();
            }
        }

        return ",";
    }

    private static string RenderText(DocumentTable table, string delimiter)
    {
        var builder = new StringBuilder();
        if (table.Headers.Count > 0)
        {
            builder.AppendLine(string.Join(delimiter, table.Headers));
        }

        foreach (DocumentTableRow row in table.Rows)
        {
            builder.AppendLine(string.Join(delimiter, row.Cells.Select(c => c.Text ?? string.Empty)));
        }

        return builder.ToString();
    }
}
