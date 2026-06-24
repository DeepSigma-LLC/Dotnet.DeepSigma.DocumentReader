namespace DeepSigma.DocumentReader;

/// <summary>Options for the HTML reader.</summary>
public sealed class HtmlReadOptions : IFormatReadOptions
{
    /// <summary>Whether to extract tables. Default <see langword="true"/>.</summary>
    public bool ExtractTables { get; init; } = true;

    /// <summary>Whether to extract links and images. Default <see langword="true"/>.</summary>
    public bool ExtractLinks { get; init; } = true;
}

/// <summary>Format-specific HTML details attached to a read result.</summary>
public sealed class HtmlDocumentFeature : IDocumentFeature
{
    /// <inheritdoc />
    public string Name => "Html";

    /// <summary>Headings in document order.</summary>
    public IReadOnlyList<DocumentHeading> Headings { get; init; } = [];

    /// <summary>Links and images in document order.</summary>
    public IReadOnlyList<DocumentLink> Links { get; init; } = [];
}
