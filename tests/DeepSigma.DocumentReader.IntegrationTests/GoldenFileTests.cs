using Xunit;

namespace DeepSigma.DocumentReader.IntegrationTests;

public sealed class GoldenFileTests
{
    [Theory]
    [InlineData("Text/sample.txt", "text", "Text/sample.expected.txt")]
    [InlineData("Json/sample.json", "json", "Json/sample.expected.json")]
    [InlineData("Json/sample.jsonl", "json", "Json/sample-jsonl.expected.json")]
    [InlineData("Csv/sample.csv", "markdown", "Csv/sample.expected.md")]
    [InlineData("Markdown/sample.md", "markdown", "Markdown/sample.expected.md")]
    public async Task Export_matches_golden(string input, string format, string expected)
        => await GoldenFile.AssertAsync(input, format, expected);
}
