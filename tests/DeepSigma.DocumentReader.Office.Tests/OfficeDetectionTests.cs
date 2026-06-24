using DeepSigma.DocumentReader.Core.Detection;
using Xunit;

namespace DeepSigma.DocumentReader.Office.Tests;

public sealed class OfficeDetectionTests
{
    private static readonly CompositeDocumentTypeDetector Detector = CompositeDocumentTypeDetector.CreateDefault();

    [Fact]
    public async Task Detects_docx_by_content_without_filename()
    {
        using var source = DocumentSource.FromBytes(OfficeSamples.CreateWord());
        var result = await Detector.DetectAsync(source);
        Assert.Equal(DocumentKind.WordDocument, result.Kind);
    }

    [Fact]
    public async Task Detects_xlsx_by_content_without_filename()
    {
        using var source = DocumentSource.FromBytes(OfficeSamples.CreateExcel());
        var result = await Detector.DetectAsync(source);
        Assert.Equal(DocumentKind.Spreadsheet, result.Kind);
    }

    [Fact]
    public async Task Detects_pptx_by_content_without_filename()
    {
        using var source = DocumentSource.FromBytes(OfficeSamples.CreatePowerPoint());
        var result = await Detector.DetectAsync(source);
        Assert.Equal(DocumentKind.Presentation, result.Kind);
    }
}
