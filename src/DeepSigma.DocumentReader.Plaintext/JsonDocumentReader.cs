using System.Text.Json;
using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Core.Text;

namespace DeepSigma.DocumentReader.Plaintext;

/// <summary>
/// Reads JSON and JSON Lines documents using <see cref="System.Text.Json"/>. Produces a
/// text projection, a flattened JSONPath view, and (for JSON Lines) per-record parsing,
/// degrading to a low-quality raw-text result rather than throwing on malformed input.
/// </summary>
public sealed class JsonDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } =
        [DocumentKind.Json, DocumentKind.JsonLines];

    /// <inheritdoc />
    protected override async Task<DocumentReadResult> ReadCoreAsync(
        DocumentReadContext context,
        CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<JsonReadOptions>();
        byte[] bytes = await TextContent.ReadAllBytesAsync(context.Stream, cancellationToken).ConfigureAwait(false);
        string text = TextContent.DecodeBomAware(bytes);

        bool jsonLinesByExtension = HasJsonLinesExtension(context.Source.FileName);
        if (jsonLinesByExtension && options.TreatJsonLinesAsRecords)
        {
            return ReadJsonLines(text, options, context);
        }

        try
        {
            return ReadSingleDocument(text, options, context);
        }
        catch (JsonException ex)
        {
            if (options.TreatJsonLinesAsRecords && LooksLikeMultipleLines(text))
            {
                return ReadJsonLines(text, options, context);
            }

            return Degrade(text, ex, context);
        }
    }

    private DocumentReadResult ReadSingleDocument(string text, JsonReadOptions options, DocumentReadContext context)
    {
        using var document = JsonDocument.Parse(text, CreateDocumentOptions(options));

        var values = options.FlattenPaths ? Flatten(document.RootElement) : [];
        string projection = options.PrettyPrint ? PrettyPrint(document.RootElement) : text;

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Json),
            Kind = DocumentKind.Json,
            Text = context.Options.ExtractText ? projection : null,
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [new JsonDocumentFeature { RootKind = document.RootElement.ValueKind, Values = values }],
        };
    }

    private DocumentReadResult ReadJsonLines(string text, JsonReadOptions options, DocumentReadContext context)
    {
        var records = new List<JsonRecord>();
        int index = 0;
        bool anyMalformed = false;

        foreach (string rawLine in EnumerateNonEmptyLines(text))
        {
            if (options.MaxRecords is { } maxRecords && index >= maxRecords)
            {
                context.AddWarning(WarningCodes.JsonMaxRecordsExceeded,
                    $"Stopped reading after the configured maximum of {maxRecords} records.");
                break;
            }

            try
            {
                using var document = JsonDocument.Parse(rawLine, CreateDocumentOptions(options));
                records.Add(new JsonRecord
                {
                    Index = index,
                    RawText = rawLine,
                    Values = options.FlattenPaths ? Flatten(document.RootElement) : [],
                });
            }
            catch (JsonException ex)
            {
                anyMalformed = true;
                context.AddWarning(WarningCodes.JsonMalformedRecord,
                    $"Skipped malformed JSON Lines record at index {index}.",
                    new DocumentLocation(Row: index),
                    ex);
            }

            index++;
        }

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.JsonLines),
            Kind = DocumentKind.JsonLines,
            Text = context.Options.ExtractText ? text : null,
            Quality = anyMalformed ? ExtractionQuality.Medium : ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [new JsonDocumentFeature { RootKind = JsonValueKind.Array, Records = records }],
        };
    }

    private static DocumentReadResult Degrade(string text, JsonException ex, DocumentReadContext context)
    {
        bool depthExceeded = ex.Message.Contains("depth", StringComparison.OrdinalIgnoreCase);
        context.AddWarning(
            depthExceeded ? WarningCodes.JsonMaxDepthExceeded : WarningCodes.JsonMalformedRecord,
            depthExceeded
                ? "JSON nesting exceeded the configured maximum depth; returning raw text."
                : "The document was not valid JSON; returning raw text.",
            exception: ex);

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Json),
            Kind = DocumentKind.Json,
            Text = context.Options.ExtractText ? text : null,
            Quality = ExtractionQuality.Low,
            Warnings = context.Warnings.ToArray(),
        };
    }

    private static JsonDocumentOptions CreateDocumentOptions(JsonReadOptions options)
        => options.MaxDepth is { } depth
            ? new JsonDocumentOptions { MaxDepth = depth }
            : new JsonDocumentOptions();

    private static IReadOnlyList<JsonPathValue> Flatten(JsonElement root)
    {
        var values = new List<JsonPathValue>();
        Flatten(root, "$", values);
        return values;
    }

    private static void Flatten(JsonElement element, string path, List<JsonPathValue> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    Flatten(property.Value, $"{path}.{property.Name}", values);
                }

                break;

            case JsonValueKind.Array:
                int i = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    Flatten(item, $"{path}[{i}]", values);
                    i++;
                }

                break;

            default:
                values.Add(new JsonPathValue(path, TextValue(element), RawValue(element), element.ValueKind));
                break;
        }
    }

    private static string? TextValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Null => null,
        _ => element.GetRawText(),
    };

    private static object? RawValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
        _ => element.GetRawText(),
    };

    private static string PrettyPrint(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            element.WriteTo(writer);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IEnumerable<string> EnumerateNonEmptyLines(string text)
    {
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }

    private static bool LooksLikeMultipleLines(string text)
        => EnumerateNonEmptyLines(text).Take(2).Count() > 1;

    private static bool HasJsonLinesExtension(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        string extension = Path.GetExtension(fileName);
        return extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ndjson", StringComparison.OrdinalIgnoreCase);
    }
}
