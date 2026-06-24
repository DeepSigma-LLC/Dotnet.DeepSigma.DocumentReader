namespace DeepSigma.DocumentReader.Core.Readers;

/// <summary>
/// Accumulates rows of string cells into a <see cref="DocumentTable"/>, assigning row and
/// column indices. Shared by the CSV, Markdown, and (later) other tabular readers.
/// </summary>
public sealed class DocumentTableBuilder
{
    private readonly List<DocumentTableRow> _rows = [];

    /// <summary>Optional table name or caption.</summary>
    public string? Name { get; set; }

    /// <summary>Optional column headers.</summary>
    public IReadOnlyList<string> Headers { get; set; } = [];

    /// <summary>Optional table location.</summary>
    public DocumentLocation? Location { get; set; }

    /// <summary>Optional confidence in the extracted structure (0–1).</summary>
    public double? Confidence { get; set; }

    /// <summary>The number of rows added so far.</summary>
    public int RowCount => _rows.Count;

    /// <summary>Appends a row of cell texts. The row index is assigned automatically.</summary>
    public void AddRow(IReadOnlyList<string?> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        int rowIndex = _rows.Count;
        var builtCells = new DocumentTableCell[cells.Count];
        for (int column = 0; column < cells.Count; column++)
        {
            builtCells[column] = new DocumentTableCell
            {
                RowIndex = rowIndex,
                ColumnIndex = column,
                Text = cells[column],
            };
        }

        _rows.Add(new DocumentTableRow { RowIndex = rowIndex, Cells = builtCells });
    }

    /// <summary>Builds the immutable table.</summary>
    public DocumentTable Build() => new()
    {
        Name = Name,
        Headers = Headers,
        Location = Location,
        Rows = _rows.ToArray(),
        Confidence = Confidence,
    };
}
