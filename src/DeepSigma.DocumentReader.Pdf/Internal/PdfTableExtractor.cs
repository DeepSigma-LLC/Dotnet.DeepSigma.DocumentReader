using UglyToad.PdfPig.Content;

namespace DeepSigma.DocumentReader.Pdf.Internal;

/// <summary>
/// Best-effort table extraction from a PDF page by clustering word bounding boxes into rows
/// (by vertical position) and columns (by horizontal alignment). Results are approximate and
/// reported with a confidence score; this is not a general-purpose table recognizer.
/// </summary>
internal static class PdfTableExtractor
{
    public readonly record struct TableResult(DocumentTable Table, double Confidence);

    /// <summary>Attempts to extract a single grid-like table from the page, or returns none.</summary>
    public static TableResult? Extract(Page page, int pageNumber)
    {
        var words = page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
        if (words.Count < 4)
        {
            return null;
        }

        double medianHeight = Median(words.Select(w => w.BoundingBox.Height));
        if (medianHeight <= 0)
        {
            return null;
        }

        List<List<Word>> lines = GroupIntoLines(words, medianHeight * 0.6);
        if (lines.Count < 2)
        {
            return null;
        }

        double medianCharWidth = Median(words.Select(w => w.BoundingBox.Width / Math.Max(1, w.Text.Length)));
        double columnGap = Math.Max(medianCharWidth * 2, 5);
        List<double> columnStarts = InferColumnStarts(lines, columnGap);
        if (columnStarts.Count < 2)
        {
            return null;
        }

        double tolerance = columnGap / 2;
        var rows = new List<DocumentTableRow>(lines.Count);
        int filledCells = 0;

        for (int r = 0; r < lines.Count; r++)
        {
            var cellText = new string?[columnStarts.Count];
            foreach (Word word in lines[r].OrderBy(w => w.BoundingBox.Left))
            {
                int column = ColumnIndexFor(word.BoundingBox.Left, columnStarts, tolerance);
                cellText[column] = cellText[column] is null ? word.Text : $"{cellText[column]} {word.Text}";
            }

            var cells = new DocumentTableCell[columnStarts.Count];
            for (int c = 0; c < columnStarts.Count; c++)
            {
                if (cellText[c] is not null)
                {
                    filledCells++;
                }

                cells[c] = new DocumentTableCell
                {
                    RowIndex = r,
                    ColumnIndex = c,
                    Text = cellText[c],
                    Location = new DocumentLocation(PageNumber: pageNumber, Row: r, Column: c),
                };
            }

            rows.Add(new DocumentTableRow { RowIndex = r, Cells = cells });
        }

        double confidence = (double)filledCells / (lines.Count * columnStarts.Count);
        var table = new DocumentTable
        {
            Location = new DocumentLocation(PageNumber: pageNumber),
            Rows = rows,
            Confidence = confidence,
        };

        return new TableResult(table, confidence);
    }

    private static List<List<Word>> GroupIntoLines(List<Word> words, double yTolerance)
    {
        var lines = new List<List<Word>>();
        List<Word>? current = null;
        double currentY = 0;

        foreach (Word word in words.OrderByDescending(VerticalCenter))
        {
            double y = VerticalCenter(word);
            if (current is null || Math.Abs(y - currentY) > yTolerance)
            {
                current = [];
                lines.Add(current);
                currentY = y;
            }

            current.Add(word);
        }

        return lines;
    }

    private static List<double> InferColumnStarts(List<List<Word>> lines, double columnGap)
    {
        var lefts = lines.SelectMany(l => l).Select(w => w.BoundingBox.Left).OrderBy(x => x).ToList();
        var starts = new List<double>();
        double? previous = null;
        foreach (double x in lefts)
        {
            if (previous is null || x - previous > columnGap)
            {
                starts.Add(x);
            }

            previous = x;
        }

        return starts;
    }

    private static int ColumnIndexFor(double left, List<double> columnStarts, double tolerance)
    {
        int index = 0;
        for (int i = 0; i < columnStarts.Count; i++)
        {
            if (left >= columnStarts[i] - tolerance)
            {
                index = i;
            }
            else
            {
                break;
            }
        }

        return index;
    }

    private static double VerticalCenter(Word word) => (word.BoundingBox.Top + word.BoundingBox.Bottom) / 2;

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
        {
            return 0;
        }

        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }
}
