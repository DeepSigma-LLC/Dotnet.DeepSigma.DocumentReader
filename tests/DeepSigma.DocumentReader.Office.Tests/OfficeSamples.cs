using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace DeepSigma.DocumentReader.Office.Tests;

/// <summary>Builds minimal Office documents in memory so reader tests need no binary fixtures.</summary>
internal static class OfficeSamples
{
    public static byte[] CreateWord()
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            MainDocumentPart main = document.AddMainDocumentPart();
            var body = new W.Body(
                Heading("Overview", 1),
                Paragraph("Revenue increased year over year."),
                Heading("Details", 2),
                Paragraph("Some details about the system."),
                new W.Table(
                    Row("Metric", "Q1", "Q2"),
                    Row("Revenue", "100", "120")));
            main.Document = new W.Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    public static byte[] CreateExcel()
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.AddWorksheet("Data");
        sheet.Cell(1, 1).Value = "name";
        sheet.Cell(1, 2).Value = "age";
        sheet.Cell(2, 1).Value = "Alice";
        sheet.Cell(2, 2).Value = 30;
        sheet.Cell(3, 1).Value = "Bob";
        sheet.Cell(3, 2).Value = 25;
        sheet.Cell(4, 1).Value = "Total";
        sheet.Cell(4, 2).FormulaA1 = "SUM(B2:B3)";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] CreatePowerPoint()
    {
        var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            PresentationPart presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();

            SlideMasterPart masterPart = presentationPart.AddNewPart<SlideMasterPart>();
            SlideLayoutPart layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
            layoutPart.SlideLayout = new P.SlideLayout(
                new P.CommonSlideData(BuildShapeTree()),
                new P.ColorMapOverride(new D.MasterColorMapping()));

            masterPart.SlideMaster = new P.SlideMaster(
                new P.CommonSlideData(BuildShapeTree()),
                DefaultColorMap(),
                new P.SlideLayoutIdList(new P.SlideLayoutId { Id = 2147483649U, RelationshipId = masterPart.GetIdOfPart(layoutPart) }));

            SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.Slide = new P.Slide(
                new P.CommonSlideData(BuildShapeTree(
                    TitleShape("Quarterly Review"),
                    BodyShape(4, "Body", "Revenue increased year over year."))),
                new P.ColorMapOverride(new D.MasterColorMapping()));
            slidePart.AddPart(layoutPart);

            NotesSlidePart notesPart = slidePart.AddNewPart<NotesSlidePart>();
            notesPart.NotesSlide = new P.NotesSlide(
                new P.CommonSlideData(BuildShapeTree(BodyShape(5, "Notes", "Discuss regional performance."))));

            presentationPart.Presentation.Append(
                new P.SlideMasterIdList(new P.SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(masterPart) }),
                new P.SlideIdList(new P.SlideId { Id = 256U, RelationshipId = presentationPart.GetIdOfPart(slidePart) }),
                new P.SlideSize { Cx = 9144000, Cy = 6858000 },
                new P.NotesSize { Cx = 6858000, Cy = 9144000 });
            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }

    private static P.ShapeTree BuildShapeTree(params OpenXmlElement[] shapes)
    {
        var tree = new P.ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1, Name = string.Empty },
                new P.NonVisualGroupShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.GroupShapeProperties());
        foreach (OpenXmlElement shape in shapes)
        {
            tree.AppendChild(shape);
        }

        return tree;
    }

    private static P.Shape TitleShape(string text)
        => new(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = 2, Name = "Title" },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = P.PlaceholderValues.Title })),
            new P.ShapeProperties(),
            new P.TextBody(new D.BodyProperties(), new D.ListStyle(), new D.Paragraph(new D.Run(new D.Text(text)))));

    private static P.Shape BodyShape(uint id, string name, string text)
        => new(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = name },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(),
            new P.TextBody(new D.BodyProperties(), new D.ListStyle(), new D.Paragraph(new D.Run(new D.Text(text)))));

    private static P.ColorMap DefaultColorMap() => new()
    {
        Background1 = D.ColorSchemeIndexValues.Light1,
        Text1 = D.ColorSchemeIndexValues.Dark1,
        Background2 = D.ColorSchemeIndexValues.Light2,
        Text2 = D.ColorSchemeIndexValues.Dark2,
        Accent1 = D.ColorSchemeIndexValues.Accent1,
        Accent2 = D.ColorSchemeIndexValues.Accent2,
        Accent3 = D.ColorSchemeIndexValues.Accent3,
        Accent4 = D.ColorSchemeIndexValues.Accent4,
        Accent5 = D.ColorSchemeIndexValues.Accent5,
        Accent6 = D.ColorSchemeIndexValues.Accent6,
        Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
        FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink,
    };

    private static W.Paragraph Heading(string text, int level)
        => new(
            new W.ParagraphProperties(new W.ParagraphStyleId { Val = $"Heading{level}" }),
            new W.Run(new W.Text(text)));

    private static W.Paragraph Paragraph(string text)
        => new(new W.Run(new W.Text(text)));

    private static W.TableRow Row(params string[] cells)
        => new(cells.Select(c => new W.TableCell(new W.Paragraph(new W.Run(new W.Text(c))))).ToArray<OpenXmlElement>());
}
