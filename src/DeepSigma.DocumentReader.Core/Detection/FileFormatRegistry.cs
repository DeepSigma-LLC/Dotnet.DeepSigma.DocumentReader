namespace DeepSigma.DocumentReader.Core.Detection;

/// <summary>
/// Static maps between file extensions, MIME content types, and <see cref="DocumentKind"/>.
/// Covers Phase 1 formats plus future kinds, so detection can surface candidates even for
/// formats no reader is registered for yet.
/// </summary>
public static class FileFormatRegistry
{
    private static readonly Dictionary<string, DocumentKind> ExtensionToKind = new(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = DocumentKind.PlainText,
        [".text"] = DocumentKind.PlainText,
        [".log"] = DocumentKind.PlainText,
        [".xml"] = DocumentKind.PlainText,
        [".md"] = DocumentKind.Markdown,
        [".markdown"] = DocumentKind.Markdown,
        [".mdown"] = DocumentKind.Markdown,
        [".mkd"] = DocumentKind.Markdown,
        [".json"] = DocumentKind.Json,
        [".jsonl"] = DocumentKind.JsonLines,
        [".ndjson"] = DocumentKind.JsonLines,
        [".csv"] = DocumentKind.Csv,
        [".tsv"] = DocumentKind.Csv,
        [".pdf"] = DocumentKind.Pdf,
        [".docx"] = DocumentKind.WordDocument,
        [".docm"] = DocumentKind.WordDocument,
        [".xlsx"] = DocumentKind.Spreadsheet,
        [".xlsm"] = DocumentKind.Spreadsheet,
        [".pptx"] = DocumentKind.Presentation,
        [".pptm"] = DocumentKind.Presentation,
        [".eml"] = DocumentKind.Email,
        [".html"] = DocumentKind.Html,
        [".htm"] = DocumentKind.Html,
        [".png"] = DocumentKind.Image,
        [".jpg"] = DocumentKind.Image,
        [".jpeg"] = DocumentKind.Image,
        [".gif"] = DocumentKind.Image,
        [".bmp"] = DocumentKind.Image,
        [".tif"] = DocumentKind.Image,
        [".tiff"] = DocumentKind.Image,
        [".webp"] = DocumentKind.Image,
        [".zip"] = DocumentKind.Archive,
    };

    private static readonly Dictionary<DocumentKind, string> KindToContentType = new()
    {
        [DocumentKind.PlainText] = "text/plain",
        [DocumentKind.Markdown] = "text/markdown",
        [DocumentKind.Json] = "application/json",
        [DocumentKind.JsonLines] = "application/x-ndjson",
        [DocumentKind.Csv] = "text/csv",
        [DocumentKind.Pdf] = "application/pdf",
        [DocumentKind.WordDocument] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [DocumentKind.Spreadsheet] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [DocumentKind.Presentation] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [DocumentKind.Email] = "message/rfc822",
        [DocumentKind.Html] = "text/html",
        [DocumentKind.Image] = "application/octet-stream",
        [DocumentKind.Archive] = "application/zip",
    };

    private static readonly Dictionary<string, DocumentKind> ContentTypeToKind = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text/plain"] = DocumentKind.PlainText,
        ["text/markdown"] = DocumentKind.Markdown,
        ["text/x-markdown"] = DocumentKind.Markdown,
        ["application/json"] = DocumentKind.Json,
        ["text/json"] = DocumentKind.Json,
        ["application/x-ndjson"] = DocumentKind.JsonLines,
        ["application/jsonl"] = DocumentKind.JsonLines,
        ["text/csv"] = DocumentKind.Csv,
        ["text/tab-separated-values"] = DocumentKind.Csv,
        ["application/pdf"] = DocumentKind.Pdf,
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = DocumentKind.WordDocument,
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = DocumentKind.Spreadsheet,
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = DocumentKind.Presentation,
        ["message/rfc822"] = DocumentKind.Email,
        ["text/html"] = DocumentKind.Html,
        ["application/zip"] = DocumentKind.Archive,
    };

    /// <summary>Maps a file extension (with or without the leading dot) to a kind, or <see cref="DocumentKind.Unknown"/>.</summary>
    public static DocumentKind KindFromExtension(string? extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return DocumentKind.Unknown;
        }

        if (extension[0] != '.')
        {
            extension = "." + extension;
        }

        return ExtensionToKind.GetValueOrDefault(extension, DocumentKind.Unknown);
    }

    /// <summary>Maps a MIME content type (ignoring any parameters) to a kind, or <see cref="DocumentKind.Unknown"/>.</summary>
    public static DocumentKind KindFromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return DocumentKind.Unknown;
        }

        int semicolon = contentType.IndexOf(';', StringComparison.Ordinal);
        string media = (semicolon >= 0 ? contentType[..semicolon] : contentType).Trim();
        return ContentTypeToKind.GetValueOrDefault(media, DocumentKind.Unknown);
    }

    /// <summary>Returns a representative MIME content type for a kind, or <see langword="null"/>.</summary>
    public static string? ContentTypeForKind(DocumentKind kind)
        => KindToContentType.GetValueOrDefault(kind);
}
