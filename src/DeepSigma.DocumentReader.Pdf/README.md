# DeepSigma.DocumentReader.Pdf

PDF reader for the DeepSigma.DocumentReader ecosystem, using PdfPig: **page-based text
extraction**, a page model, and basic metadata. Best-effort only — scanned PDFs with no text
layer are flagged with a warning rather than returning empty text. OCR is not applied.

```bash
dotnet add package DeepSigma.DocumentReader.Pdf
```

## Register

```csharp
builder.Services.AddDeepSigmaDocumentReader().AddPdf();
```

## Highlights

- Content-order page text + a `DocumentPage` per page; `MaxPages` support.
- Metadata (title, author, page count, parsed creation/modification dates).
- Scanned pages → `Pdf.TextLayerMissing` warning and reduced quality.
- **Opt-in** best-effort table extraction (`PdfReadOptions { ExtractTables = true }`) by
  clustering word coordinates, reported with a confidence score.

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
