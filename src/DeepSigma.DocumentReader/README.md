# DeepSigma.DocumentReader

Convenience meta-package for **DeepSigma.DocumentReader** — one consistent API to read many
document formats (plain text, Markdown, JSON/JSONL, CSV, Word, Excel, PowerPoint, PDF, HTML,
email) into a unified result model.

This package references the contracts, the orchestration core, every managed reader, and the
exporters, and adds a one-call factory plus default dependency-injection registration. (OCR
is intentionally excluded so no native binaries are pulled in.)

```bash
dotnet add package DeepSigma.DocumentReader
```

## Quick start

```csharp
using DeepSigma.DocumentReader;

IDocumentReader reader = DocumentReaderFactory.CreateDefault();

using DocumentSource source = DocumentSource.FromFile("invoice.pdf");
DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

Console.WriteLine(result.Kind);   // e.g. Pdf
Console.WriteLine(result.Text);
```

## Dependency injection

```csharp
builder.Services.AddDeepSigmaDocumentReaderDefaults();
// inject IDocumentReader (and IExporterResolver) anywhere
```

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme)
for options, exporters, attachments, detection, and more.
