namespace DeepSigma.DocumentReader;

/// <summary>Options for the Markdown reader.</summary>
public sealed class MarkdownReadOptions : IFormatReadOptions
{
    /// <summary>
    /// When <see langword="true"/> (default), the text projection preserves the original
    /// Markdown; otherwise it is rendered to plain text.
    /// </summary>
    public bool PreserveMarkdown { get; init; } = true;

    /// <summary>Whether to parse YAML front matter. Default <see langword="true"/>.</summary>
    public bool ExtractFrontMatter { get; init; } = true;

    /// <summary>Whether to collect code blocks. Default <see langword="true"/>.</summary>
    public bool ExtractCodeBlocks { get; init; } = true;

    /// <summary>Whether to collect links and images. Default <see langword="true"/>.</summary>
    public bool ExtractLinks { get; init; } = true;
}

/// <summary>A heading extracted from a Markdown document.</summary>
/// <param name="Level">The heading level (1–6).</param>
/// <param name="Text">The heading text.</param>
public sealed record DocumentHeading(int Level, string Text);

/// <summary>A fenced or indented code block.</summary>
/// <param name="Language">The fenced code info string (language), if any.</param>
/// <param name="Code">The code content.</param>
public sealed record DocumentCodeBlock(string? Language, string Code);

/// <summary>A link or image reference.</summary>
/// <param name="Text">The link/alt text, if any.</param>
/// <param name="Url">The target URL.</param>
/// <param name="IsImage">Whether the reference is an image.</param>
public sealed record DocumentLink(string? Text, string Url, bool IsImage);

/// <summary>Format-specific Markdown details attached to a read result.</summary>
public sealed class MarkdownDocumentFeature : IDocumentFeature
{
    /// <inheritdoc />
    public string Name => "Markdown";

    /// <summary>Headings in document order.</summary>
    public IReadOnlyList<DocumentHeading> Headings { get; init; } = [];

    /// <summary>Code blocks in document order.</summary>
    public IReadOnlyList<DocumentCodeBlock> CodeBlocks { get; init; } = [];

    /// <summary>Links and images in document order.</summary>
    public IReadOnlyList<DocumentLink> Links { get; init; } = [];

    /// <summary>Parsed YAML front matter (string values), if present.</summary>
    public IReadOnlyDictionary<string, string> FrontMatter { get; init; }
        = new Dictionary<string, string>();
}
