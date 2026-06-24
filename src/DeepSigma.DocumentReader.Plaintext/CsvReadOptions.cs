using System.Globalization;

namespace DeepSigma.DocumentReader;

/// <summary>Options for the CSV reader.</summary>
public sealed class CsvReadOptions : IFormatReadOptions
{
    /// <summary>Whether the first row is a header. Default <see langword="true"/>.</summary>
    public bool HasHeaderRecord { get; init; } = true;

    /// <summary>An explicit delimiter. When <see langword="null"/>, the delimiter is detected.</summary>
    public string? Delimiter { get; init; }

    /// <summary>The culture used for parsing. Default <see cref="CultureInfo.InvariantCulture"/>.</summary>
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

    /// <summary>Maximum number of data rows to read. Default 5,000,000.</summary>
    public int? MaxRows { get; init; } = 5_000_000;
}
