using System.Text;

namespace DeepSigma.DocumentReader;

/// <summary>Options for the plain-text reader.</summary>
public sealed class TextReadOptions : IFormatReadOptions
{
    /// <summary>An explicit encoding to use. When set, encoding detection is skipped.</summary>
    public Encoding? Encoding { get; init; }

    /// <summary>Whether to detect the encoding from a byte-order mark / content. Default <see langword="true"/>.</summary>
    public bool DetectEncoding { get; init; } = true;

    /// <summary>Whether to normalize line endings to LF. Default <see langword="true"/>.</summary>
    public bool NormalizeLineEndings { get; init; } = true;

    /// <summary>Maximum number of characters to return; content beyond this is truncated.</summary>
    public long? MaxCharacters { get; init; }
}
