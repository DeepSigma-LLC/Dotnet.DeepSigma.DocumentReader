using System.Text;
using DeepSigma.DocumentReader.Plaintext;
using Xunit;

namespace DeepSigma.DocumentReader.Plaintext.Tests;

public sealed class TextDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync(byte[] bytes, DocumentReadOptions? options = null)
    {
        var reader = new TextDocumentReader();
        using var source = DocumentSource.FromBytes(bytes, "sample.txt");
        return await reader.ReadAsync(source, options ?? DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Reads_utf8_with_bom()
    {
        byte[] bom = [0xEF, 0xBB, 0xBF];
        byte[] bytes = [.. bom, .. Encoding.UTF8.GetBytes("héllo")];

        var result = await ReadAsync(bytes);

        Assert.Equal("héllo", result.Text);
        Assert.Equal(ExtractionQuality.High, result.Quality);
    }

    [Fact]
    public async Task Normalizes_line_endings()
    {
        var result = await ReadAsync(Encoding.UTF8.GetBytes("a\r\nb\rc\n"));
        Assert.Equal("a\nb\nc\n", result.Text);
    }

    [Fact]
    public async Task Falls_back_to_latin1_with_warning_on_invalid_utf8()
    {
        // 0xFF is invalid as standalone UTF-8.
        byte[] bytes = [0x68, 0x69, 0xFF];

        var result = await ReadAsync(bytes);

        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.UnsupportedEncoding);
        Assert.StartsWith("hi", result.Text);
    }

    [Fact]
    public async Task Truncates_to_max_characters_with_warning()
    {
        var options = DocumentReadOptions.Default.WithOptions(new TextReadOptions { MaxCharacters = 3 });
        var result = await ReadAsync(Encoding.UTF8.GetBytes("abcdef"), options);

        Assert.Equal("abc", result.Text);
        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.TextTruncated);
    }
}
