using System.Text;
using System.Text.RegularExpressions;
using DeepSigma.DocumentReader;
using DeepSigma.DocumentReader.Export;
using Xunit;

namespace DeepSigma.DocumentReader.IntegrationTests;

/// <summary>
/// Reads a corpus file, exports it, normalizes volatile values, and compares against a
/// committed golden file. Set the environment variable <c>UPDATE_GOLDEN=1</c> to regenerate
/// the golden files instead of asserting.
/// </summary>
internal static partial class GoldenFile
{
    private static readonly IDocumentReader Reader = DocumentReaderFactory.CreateDefault();
    private static readonly ExporterResolver Exporters = ExporterResolver.CreateDefault();

    public static async Task AssertAsync(string inputRelativePath, string format, string expectedRelativePath)
    {
        string inputPath = TestPaths.Corpus(inputRelativePath.Split('/'));
        string expectedPath = TestPaths.Corpus(expectedRelativePath.Split('/'));

        using var source = DocumentSource.FromFile(inputPath);
        DocumentReadResult result = await Reader.ReadAsync(source, DocumentReadOptions.Default);

        IDocumentResultExporter exporter = Exporters.Resolve(format)!;
        using var buffer = new MemoryStream();
        await exporter.ExportAsync(result, buffer);
        string actual = Normalize(Encoding.UTF8.GetString(buffer.ToArray()));

        if (ShouldUpdate)
        {
            await File.WriteAllTextAsync(expectedPath, actual);
            return;
        }

        Assert.True(File.Exists(expectedPath), $"Missing golden file: {expectedPath}. Run with UPDATE_GOLDEN=1 to create it.");
        string expected = Normalize(await File.ReadAllTextAsync(expectedPath));
        Assert.Equal(expected, actual);
    }

    private static bool ShouldUpdate
        => string.Equals(Environment.GetEnvironmentVariable("UPDATE_GOLDEN"), "1", StringComparison.Ordinal);

    /// <summary>Replaces volatile values (sizes) and normalizes line endings for stable comparison.</summary>
    private static string Normalize(string content)
    {
        content = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        content = SizeBytesRegex().Replace(content, "\"sizeBytes\": 0");
        return content;
    }

    [GeneratedRegex("\"sizeBytes\":\\s*\\d+")]
    private static partial Regex SizeBytesRegex();
}
