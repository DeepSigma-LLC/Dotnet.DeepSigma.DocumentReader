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

    /// <summary>Creates a single-page PDF whose text is laid out as a 3×3 grid.</summary>
    public static byte[] CreateGrid()
    {
        var builder = new PdfDocumentBuilder();
        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfPageBuilder page = builder.AddPage(PageSize.A4);

        double[] columnX = [50, 220, 390];
        double[] rowY = [700, 670, 640];
        string[][] grid =
        [
            ["Name", "Age", "City"],
            ["Alice", "30", "London"],
            ["Bob", "25", "Paris"],
        ];

        for (int r = 0; r < grid.Length; r++)
        {
            for (int c = 0; c < grid[r].Length; c++)
            {
                page.AddText(grid[r][c], 10, new PdfPoint(columnX[c], rowY[r]), font);
            }
        }

        return builder.Build();
    }
}
