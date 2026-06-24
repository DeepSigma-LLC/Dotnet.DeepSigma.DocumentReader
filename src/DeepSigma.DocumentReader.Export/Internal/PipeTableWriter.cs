using System.Text;

namespace DeepSigma.DocumentReader.Export.Internal;

/// <summary>Renders a <see cref="DocumentTable"/> as a GitHub-flavored Markdown pipe table.</summary>
internal static class PipeTableWriter
{
    public static void Write(StringBuilder builder, DocumentTable table)
    {
        int columnCount = ColumnCount(table);
        if (columnCount == 0)
        {
            return;
        }

        IReadOnlyList<string> headers = table.Headers.Count > 0
            ? table.Headers
            : Enumerable.Range(1, columnCount).Select(i => $"Column {i}").ToArray();

        WriteRow(builder, headers.Select(Escape), columnCount);
        WriteRow(builder, Enumerable.Repeat("---", columnCount), columnCount);

        foreach (DocumentTableRow row in table.Rows)
        {
            var cells = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                string? text = i < row.Cells.Count ? row.Cells[i].Text : null;
                cells[i] = Escape(text ?? string.Empty);
            }

            WriteRow(builder, cells, columnCount);
        }
    }

    private static int ColumnCount(DocumentTable table)
    {
        int count = table.Headers.Count;
        foreach (DocumentTableRow row in table.Rows)
        {
            count = Math.Max(count, row.Cells.Count);
        }

        return count;
    }

    private static void WriteRow(StringBuilder builder, IEnumerable<string> cells, int columnCount)
    {
        var padded = cells.ToList();
        while (padded.Count < columnCount)
        {
            padded.Add(string.Empty);
        }

        builder.Append("| ").Append(string.Join(" | ", padded)).AppendLine(" |");
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');
}
