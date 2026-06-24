using System.Text;
using System.Text.Json;
using DeepSigma.DocumentReader.Plaintext;
using Xunit;

namespace DeepSigma.DocumentReader.Plaintext.Tests;

public sealed class JsonDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync(string content, string fileName = "data.json", DocumentReadOptions? options = null)
    {
        var reader = new JsonDocumentReader();
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes(content), fileName);
        return await reader.ReadAsync(source, options ?? DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Flattens_paths_for_a_single_document()
    {
        var result = await ReadAsync("{\"customer\":{\"name\":\"Acme\"},\"items\":[{\"sku\":\"A\"}]}");

        var feature = result.GetFeature<JsonDocumentFeature>();
        Assert.NotNull(feature);
        Assert.Equal(JsonValueKind.Object, feature!.RootKind);
        Assert.Contains(feature.Values, v => v.Path == "$.customer.name" && (string?)v.RawValue == "Acme");
        Assert.Contains(feature.Values, v => v.Path == "$.items[0].sku");
    }

    [Fact]
    public async Task Reads_json_lines_records()
    {
        var result = await ReadAsync("{\"id\":1}\n{\"id\":2}\n{\"id\":3}\n", "data.jsonl");

        Assert.Equal(DocumentKind.JsonLines, result.Kind);
        var feature = result.GetFeature<JsonDocumentFeature>();
        Assert.Equal(3, feature!.Records.Count);
    }

    [Fact]
    public async Task Skips_malformed_json_lines_record_with_warning()
    {
        var result = await ReadAsync("{\"id\":1}\nnot json\n{\"id\":3}\n", "data.jsonl");

        var feature = result.GetFeature<JsonDocumentFeature>();
        Assert.Equal(2, feature!.Records.Count);
        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.JsonMalformedRecord);
        Assert.Equal(ExtractionQuality.Medium, result.Quality);
    }

    [Fact]
    public async Task Degrades_on_depth_exceeded_with_warning()
    {
        var options = DocumentReadOptions.Default.WithOptions(new JsonReadOptions { MaxDepth = 2 });
        var result = await ReadAsync("{\"a\":{\"b\":{\"c\":{\"d\":1}}}}", options: options);

        Assert.Equal(ExtractionQuality.Low, result.Quality);
        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.JsonMaxDepthExceeded);
    }
}
