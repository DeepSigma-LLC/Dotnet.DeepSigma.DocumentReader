using System.Text.Json;

namespace DeepSigma.DocumentReader;

/// <summary>Options for the JSON / JSON Lines reader.</summary>
public sealed class JsonReadOptions : IFormatReadOptions
{
    /// <summary>Whether the text projection is pretty-printed. Default <see langword="true"/>.</summary>
    public bool PrettyPrint { get; init; } = true;

    /// <summary>Whether to produce flattened JSONPath values. Default <see langword="true"/>.</summary>
    public bool FlattenPaths { get; init; } = true;

    /// <summary>Whether JSON Lines input is parsed into per-line records. Default <see langword="true"/>.</summary>
    public bool TreatJsonLinesAsRecords { get; init; } = true;

    /// <summary>Maximum nesting depth before parsing fails with a warning. Default 64.</summary>
    public int? MaxDepth { get; init; } = 64;

    /// <summary>Maximum number of JSON Lines records to read. Default 1,000,000.</summary>
    public int? MaxRecords { get; init; } = 1_000_000;
}

/// <summary>A single value located at a JSONPath within a document.</summary>
/// <param name="Path">The JSONPath, e.g. <c>$.items[0].sku</c>.</param>
/// <param name="TextValue">The value's text projection.</param>
/// <param name="RawValue">The value's CLR representation (string, long, double, bool, or null).</param>
/// <param name="ValueKind">The JSON value kind.</param>
public sealed record JsonPathValue(string Path, string? TextValue, object? RawValue, JsonValueKind ValueKind);

/// <summary>A single JSON Lines record.</summary>
public sealed class JsonRecord
{
    /// <summary>The 0-based record index.</summary>
    public required int Index { get; init; }

    /// <summary>The record's raw JSON text.</summary>
    public string? RawText { get; init; }

    /// <summary>The record's flattened values, when flattening is enabled.</summary>
    public IReadOnlyList<JsonPathValue> Values { get; init; } = [];
}

/// <summary>Format-specific JSON details attached to a read result.</summary>
public sealed class JsonDocumentFeature : IDocumentFeature
{
    /// <inheritdoc />
    public string Name => "Json";

    /// <summary>The kind of the JSON root value.</summary>
    public JsonValueKind RootKind { get; init; }

    /// <summary>Flattened values for a single JSON document.</summary>
    public IReadOnlyList<JsonPathValue> Values { get; init; } = [];

    /// <summary>Parsed records for JSON Lines input.</summary>
    public IReadOnlyList<JsonRecord> Records { get; init; } = [];
}
