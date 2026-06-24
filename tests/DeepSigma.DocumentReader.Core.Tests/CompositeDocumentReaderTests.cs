using System.Text;
using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using Xunit;

namespace DeepSigma.DocumentReader.Core.Tests;

public sealed class CompositeDocumentReaderTests
{
    private sealed class FakeReader(DocumentKind kind, string marker) : IFormatDocumentReader
    {
        public IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [kind];
        public bool CanRead(DocumentSource source) => true;
        public int GetConfidence(DocumentSource source, DocumentTypeDetectionResult detectionResult)
            => SupportedKinds.Contains(detectionResult.Kind) ? detectionResult.Confidence : 0;

        public Task<DocumentReadResult> ReadAsync(DocumentSource source, DocumentReadOptions options, CancellationToken cancellationToken = default)
            => Task.FromResult(new DocumentReadResult
            {
                Source = new DocumentSourceInfo { DetectedKind = kind },
                Kind = kind,
                Text = marker,
            });
    }

    private static CompositeDocumentReader Create(params IFormatDocumentReader[] readers)
        => new(readers, CompositeDocumentTypeDetector.CreateDefault());

    [Fact]
    public async Task Routes_to_the_reader_matching_the_detected_kind()
    {
        var reader = Create(new FakeReader(DocumentKind.Json, "json-reader"), new FakeReader(DocumentKind.Csv, "csv-reader"));
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes("{\"a\":1}"), "data.json");

        var result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal("json-reader", result.Text);
    }

    [Fact]
    public async Task Throws_when_no_reader_supports_the_detected_kind()
    {
        var reader = Create(new FakeReader(DocumentKind.Csv, "csv-reader"));
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes("{\"a\":1}"), "data.json");

        await Assert.ThrowsAsync<UnsupportedDocumentTypeException>(
            async () => await reader.ReadAsync(source, DocumentReadOptions.Default));
    }
}
