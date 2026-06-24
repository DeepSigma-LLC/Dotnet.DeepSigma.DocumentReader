namespace DeepSigma.DocumentReader.Office;

/// <summary>Shared helpers for mapping Office package metadata into the unified model.</summary>
internal static class OfficeMetadata
{
    /// <summary>
    /// Maps OPC package core properties (shared by DOCX and PPTX) to document metadata. Takes
    /// primitives so callers read them via the package's (experimental) properties type.
    /// </summary>
    public static DocumentMetadata FromCoreProperties(
        string? title, string? creator, DateTime? created, DateTime? modified, string? language) => new()
    {
        Title = string.IsNullOrEmpty(title) ? null : title,
        Author = string.IsNullOrEmpty(creator) ? null : creator,
        CreatedUtc = ToOffset(created),
        ModifiedUtc = ToOffset(modified),
        Language = string.IsNullOrEmpty(language) ? null : language,
    };

    /// <summary>
    /// Converts an Open XML <see cref="DateTime"/> (kind often unspecified) to a
    /// <see cref="DateTimeOffset"/> treated as UTC for deterministic results.
    /// </summary>
    public static DateTimeOffset? ToOffset(DateTime? value)
        => value is { } dateTime
            ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
            : null;
}
