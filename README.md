# DeepSigma.DocumentReader

A .NET 10 library that gives consuming applications **one consistent API** to read many
document formats — extracting text, tables, sections, metadata, and warnings through a
unified result model, while hiding format-specific parsing behind modular packages.

```csharp
using DeepSigma.DocumentReader;

IDocumentReader reader = DocumentReaderFactory.CreateDefault();

using DocumentSource source = DocumentSource.FromFile("invoice.csv");
DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

Console.WriteLine(result.Kind);   // e.g. Csv
Console.WriteLine(result.Text);
```

The consumer does not need to know whether the file is plain text, Markdown, JSON, or CSV —
the library detects the type and routes to the right reader.

## Status

**Phases 1–2** are implemented: the contracts, orchestration core, the text-family readers,
the Office readers, exporters, and a CLI. PDF, email, HTML, and OCR readers are planned for
later phases (see [docs/DesignGuidance.md](docs/DesignGuidance.md)).

Implemented formats: **plain text, Markdown, JSON / JSON Lines, CSV, Word (DOCX), Excel
(XLSX), PowerPoint (PPTX)**.

## Packages

Packages are grouped by *dependency tree*, so a consumer only pulls the dependencies it uses.

| Package | Purpose | External deps |
|---|---|---|
| `DeepSigma.DocumentReader.Abstractions` | Contracts only (interfaces, DTOs, enums, options, exceptions) | none |
| `DeepSigma.DocumentReader.Core` | Type detection, composite reader, stream handling, DI, shared reader base/builders | DI abstractions |
| `DeepSigma.DocumentReader.Plaintext` | Text, Markdown, JSON/JSONL, CSV readers | Markdig, CsvHelper |
| `DeepSigma.DocumentReader.Office` | Word (DOCX), Excel (XLSX), PowerPoint (PPTX) readers | DocumentFormat.OpenXml, ClosedXML |
| `DeepSigma.DocumentReader.Export` | Text / Markdown / JSON exporters | none |
| `DeepSigma.DocumentReader` | Convenience meta-package: factory + DI defaults | (the above) |
| `DeepSigma.DocumentReader.Cli` | `dsread` command-line tool | System.CommandLine |

Planned: `Pdf`, `Email`, `Html`, `Ocr`.

## Dependency injection

```csharp
builder.Services.AddDeepSigmaDocumentReaderDefaults();
// or compose explicitly:
builder.Services.AddDeepSigmaDocumentReader().AddText().AddJson().AddCsv().AddMarkdown().AddOffice();
```

## CLI (`dsread`)

```bash
dsread detect  input.json
dsread extract input.md  --format json --output out.json
dsread inspect input.csv
dsread batch   ./inputs --output ./out --format markdown --recursive
```

## Repository layout

```
src/        the packages above
tests/      unit tests, integration tests, and a shared Corpus/ of sample + golden files
samples/    runnable usage examples
docs/       design guidance
```

## Building & testing

```bash
dotnet build DeepSigma.DocumentReader.slnx
dotnet test  DeepSigma.DocumentReader.slnx
```

Golden-file expectations live next to their inputs under `tests/Corpus/`. To regenerate them
after an intentional output change, run the tests with `UPDATE_GOLDEN=1`.
