using DeepSigma.DocumentReader.Office;
using Xunit;

namespace DeepSigma.DocumentReader.Office.Tests;

public sealed class PowerPointDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync()
    {
        var reader = new PowerPointDocumentReader();
        using var source = DocumentSource.FromBytes(OfficeSamples.CreatePowerPoint(), "sample.pptx");
        return await reader.ReadAsync(source, DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Extracts_slide_title_text_and_notes()
    {
        DocumentReadResult result = await ReadAsync();

        Assert.Equal(DocumentKind.Presentation, result.Kind);
        var feature = result.GetFeature<PresentationDocumentFeature>();
        Assert.NotNull(feature);

        PresentationSlide slide = Assert.Single(feature!.Slides);
        Assert.Equal("Quarterly Review", slide.Title);
        Assert.Contains("Revenue increased", slide.Text, StringComparison.Ordinal);
        Assert.Contains("regional performance", slide.SpeakerNotes, StringComparison.Ordinal);
    }
}
