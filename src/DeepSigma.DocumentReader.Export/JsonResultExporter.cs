using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSigma.DocumentReader.Export;

/// <summary>Exports the full <see cref="DocumentReadResult"/> as indented JSON.</summary>
public sealed class JsonResultExporter : IDocumentResultExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    /// <inheritdoc />
    public string Format => "json";

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public async Task ExportAsync(DocumentReadResult result, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(output);
        await JsonSerializer.SerializeAsync(output, result, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new DocumentFeatureConverter());
        return options;
    }

    /// <summary>
    /// Serializes each <see cref="IDocumentFeature"/> by its concrete runtime type so
    /// format-specific properties are captured (the interface alone would expose only Name).
    /// </summary>
    private sealed class DocumentFeatureConverter : JsonConverter<IDocumentFeature>
    {
        public override IDocumentFeature Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Features are write-only in the JSON export.");

        public override void Write(Utf8JsonWriter writer, IDocumentFeature value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
