namespace DeepSigma.DocumentReader.Office;

/// <summary>Shared helpers for mapping Office package metadata into the unified model.</summary>
internal static class OfficeMetadata
{
    /// <summary>
    /// Converts an Open XML <see cref="DateTime"/> (kind often unspecified) to a
    /// <see cref="DateTimeOffset"/> treated as UTC for deterministic results.
    /// </summary>
    public static DateTimeOffset? ToOffset(DateTime? value)
        => value is { } dateTime
            ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
            : null;
}
