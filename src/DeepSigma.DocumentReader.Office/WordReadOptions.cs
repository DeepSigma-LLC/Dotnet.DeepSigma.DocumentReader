namespace DeepSigma.DocumentReader;

/// <summary>Options for the Word (DOCX) reader.</summary>
public sealed class WordReadOptions : IFormatReadOptions
{
    /// <summary>Whether to append header and footer text. Default <see langword="false"/>.</summary>
    public bool IncludeHeadersAndFooters { get; init; }

    /// <summary>Whether to append footnote and endnote text. Default <see langword="false"/>.</summary>
    public bool IncludeFootnotes { get; init; }

    /// <summary>Whether to append comment text. Default <see langword="false"/>.</summary>
    public bool IncludeComments { get; init; }

    /// <summary>Whether to include tracked-change deleted text. Default <see langword="false"/>.</summary>
    public bool IncludeDeletedText { get; init; }

    /// <summary>Whether to extract tables. Default <see langword="true"/>.</summary>
    public bool ExtractTables { get; init; } = true;
}
