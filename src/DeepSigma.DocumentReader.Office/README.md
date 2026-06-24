# DeepSigma.DocumentReader.Office

Readers for **Office Open XML** documents in the DeepSigma.DocumentReader ecosystem: Word
(DOCX), Excel (XLSX), and PowerPoint (PPTX). Uses the Open XML SDK (Word/PowerPoint) and
ClosedXML (Excel). **Macros are never executed.**

```bash
dotnet add package DeepSigma.DocumentReader.Office
```

## Register

```csharp
builder.Services.AddDeepSigmaDocumentReader().AddOffice();
// or individually: .AddWord() / .AddExcel() / .AddPowerPoint()
```

## Highlights

- **Word** — paragraph prose, heading→section tree, tables, core metadata; optional
  headers/footers, footnotes/endnotes, comments, tracked-change text.
- **Excel** — per-sheet `DocumentTable` with **typed cell values** and cell addresses; warns
  that formulas use cached values (not recalculated).
- **PowerPoint** — per-slide title, body text, speaker notes, and tables; skips hidden slides.

Options: `WordReadOptions`, `ExcelReadOptions`, `PowerPointReadOptions`. Features:
`SpreadsheetDocumentFeature`, `PresentationDocumentFeature`.

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
