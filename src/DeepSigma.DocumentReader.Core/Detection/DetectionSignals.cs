namespace DeepSigma.DocumentReader.Core.Detection;

/// <summary>Suggests a kind from an explicitly supplied MIME content type (highest trust).</summary>
public sealed class ContentTypeSignal : IDetectionSignal
{
    /// <inheritdoc />
    public string Name => "content-type";

    /// <inheritdoc />
    public void Evaluate(DetectionContext context)
    {
        var kind = FileFormatRegistry.KindFromContentType(context.ContentType);
        if (kind != DocumentKind.Unknown)
        {
            context.AddCandidate(kind, 90, Name);
        }
    }
}

/// <summary>Suggests a kind from the file extension.</summary>
public sealed class ExtensionSignal : IDetectionSignal
{
    /// <inheritdoc />
    public string Name => "extension";

    /// <inheritdoc />
    public void Evaluate(DetectionContext context)
    {
        var kind = FileFormatRegistry.KindFromExtension(context.Extension);
        if (kind != DocumentKind.Unknown)
        {
            context.AddCandidate(kind, 70, Name);
        }
    }
}

/// <summary>Suggests a kind from leading magic bytes / byte-order marks.</summary>
public sealed class MagicBytesSignal : IDetectionSignal
{
    /// <inheritdoc />
    public string Name => "magic-bytes";

    /// <inheritdoc />
    public void Evaluate(DetectionContext context)
    {
        var bytes = context.Prefix.Span;
        if (bytes.Length >= 5 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F' && bytes[4] == '-')
        {
            context.AddCandidate(DocumentKind.Pdf, 90, Name);
            return;
        }

        // OOXML and other ZIP-based containers share the PK\x03\x04 signature.
        if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
        {
            context.AddCandidate(DocumentKind.Archive, 50, Name);
            return;
        }

        // A UTF byte-order mark indicates a text-family document.
        bool hasBom =
            (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) ||
            (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) ||
            (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF);
        if (hasBom)
        {
            context.AddCandidate(DocumentKind.PlainText, 25, Name);
        }
    }
}

/// <summary>
/// Sniffs the decoded prefix for structural cues (JSON, Markdown, CSV) and always offers
/// plain text as a low-confidence universal fallback.
/// </summary>
public sealed class ContentSniffSignal : IDetectionSignal
{
    /// <inheritdoc />
    public string Name => "content-sniff";

    /// <inheritdoc />
    public void Evaluate(DetectionContext context)
    {
        string text = context.PrefixText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Plain text is always a viable fallback so text-family input never resolves to Unknown.
        context.AddCandidate(DocumentKind.PlainText, 10, Name);

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Take(20)
            .ToList();

        string trimmedStart = text.TrimStart();

        if (trimmedStart.Length > 0 && (trimmedStart[0] == '{' || trimmedStart[0] == '['))
        {
            int objectLines = lines.Count(l => l.StartsWith('{') || l.StartsWith('['));
            if (objectLines > 1)
            {
                context.AddCandidate(DocumentKind.JsonLines, 50, Name);
                context.AddCandidate(DocumentKind.Json, 35, Name);
            }
            else
            {
                context.AddCandidate(DocumentKind.Json, 55, Name);
            }

            return;
        }

        if (IsHtml(trimmedStart))
        {
            context.AddCandidate(DocumentKind.Html, 55, Name);
            return;
        }

        if (LooksLikeEmail(lines, text))
        {
            context.AddCandidate(DocumentKind.Email, 55, Name);
            return;
        }

        bool hasFrontMatter = trimmedStart.StartsWith("---\n", StringComparison.Ordinal)
            || trimmedStart.StartsWith("---\r\n", StringComparison.Ordinal);
        bool hasHeading = lines.Any(l => IsMarkdownHeading(l));
        bool hasTableSeparator = lines.Any(l => l.StartsWith("|", StringComparison.Ordinal) && l.Contains("---", StringComparison.Ordinal));
        if (hasFrontMatter || hasHeading || hasTableSeparator)
        {
            context.AddCandidate(DocumentKind.Markdown, 55, Name);
            return;
        }

        if (LooksLikeDelimited(lines))
        {
            context.AddCandidate(DocumentKind.Csv, 50, Name);
        }
    }

    private static bool IsMarkdownHeading(string line)
    {
        int hashes = 0;
        while (hashes < line.Length && line[hashes] == '#')
        {
            hashes++;
        }

        return hashes is >= 1 and <= 6 && hashes < line.Length && line[hashes] == ' ';
    }

    private static bool IsHtml(string trimmedStart)
    {
        if (trimmedStart.Length == 0 || trimmedStart[0] != '<')
        {
            return false;
        }

        string head = (trimmedStart.Length > 512 ? trimmedStart[..512] : trimmedStart).ToLowerInvariant();
        return head.Contains("<html", StringComparison.Ordinal)
            || head.Contains("<!doctype html", StringComparison.Ordinal)
            || head.Contains("<head", StringComparison.Ordinal)
            || head.Contains("<body", StringComparison.Ordinal);
    }

    private static bool LooksLikeEmail(List<string> lines, string text)
    {
        if (lines.Count == 0 || !IsHeaderLine(lines[0]))
        {
            return false;
        }

        string lower = (text.Length > 2048 ? text[..2048] : text).ToLowerInvariant();
        bool hasFrom = lower.StartsWith("from:", StringComparison.Ordinal) || lower.Contains("\nfrom:", StringComparison.Ordinal);
        bool corroborating = lower.Contains("mime-version:", StringComparison.Ordinal)
            || lower.Contains("\nreceived:", StringComparison.Ordinal)
            || lower.Contains("\nsubject:", StringComparison.Ordinal)
            || lower.Contains("\nto:", StringComparison.Ordinal);
        return hasFrom && corroborating;
    }

    private static bool IsHeaderLine(string line)
    {
        int colon = line.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0)
        {
            return false;
        }

        foreach (char c in line.AsSpan(0, colon))
        {
            if (c <= ' ')
            {
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikeDelimited(List<string> lines)
    {
        if (lines.Count < 2)
        {
            return false;
        }

        foreach (char delimiter in (ReadOnlySpan<char>)[',', ';', '\t', '|'])
        {
            int first = lines[0].Count(c => c == delimiter);
            if (first >= 1 && lines.All(l => l.Count(c => c == delimiter) == first))
            {
                return true;
            }
        }

        return false;
    }
}
