using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DeepSigma.DocumentReader.Pdf.Tests;

/// <summary>Builds PDFs in memory so reader tests need no binary fixtures.</summary>
internal static class PdfSamples
{
    /// <summary>Creates a PDF with <paramref name="pageCount"/> pages, each containing one line of text.</summary>
    public static byte[] CreateWithText(int pageCount = 1)
    {
        var builder = new PdfDocumentBuilder();
        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (int i = 1; i <= pageCount; i++)
        {
            PdfPageBuilder page = builder.AddPage(PageSize.A4);
            page.AddText($"Hello from page {i}.", 12, new PdfPoint(25, 700), font);
        }

        return builder.Build();
    }

    /// <summary>Creates a single-page PDF with no text layer (simulating a scanned page).</summary>
    public static byte[] CreateWithoutText()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        return builder.Build();
    }
}
