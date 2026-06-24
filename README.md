# DeepSigma.DocumentReader

A .NET 10 library that gives consuming applications **one consistent API** to read many
document formats — extracting text, sections, tables, metadata, attachments, and warnings
through a unified result model, while hiding format-specific parsing behind modular packages.

The consumer never has to know whether a file is plain text, JSON, CSV, a Word document, a
PDF, an email, or HTML — the library detects the type and routes to the right reader.

**Implemented formats:** plain text, Markdown, JSON / JSON Lines, CSV, Word (DOCX),
Excel (XLSX), PowerPoint (PPTX), PDF, HTML, email (`.eml`).

---

## Contents

- [Install](#install)
- [Quick start](#quick-start)
- [Dependency injection](#dependency-injection)
- [Reading from files, streams, and bytes](#reading-from-files-streams-and-bytes)
- [Working with the result](#working-with-the-result)
- [Format-specific options](#format-specific-options)
- [Exporting a result](#exporting-a-result)
- [Email attachments](#email-attachments)
- [Detecting a document's type](#detecting-a-documents-type)
- [Security and resource limits](#security-and-resource-limits)
- [Command-line tool (`dsread`)](#command-line-tool-dsread)
- [Extending with a custom reader](#extending-with-a-custom-reader)
- [Packages](#packages)
- [Repository layout](#repository-layout)
- [Building and testing](#building-and-testing)

---

## Install

The convenience meta-package pulls in the core plus every managed reader (text, Office, PDF,
HTML, email) and the exporters:

```bash
dotnet add package DeepSigma.DocumentReader
```

Or reference only what you need — e.g. just the contracts and CSV/JSON:

```bash
dotnet add package DeepSigma.DocumentReader.Core
dotnet add package DeepSigma.DocumentReader.Plaintext
```

---

## Quick start

No dependency-injection setup required:

```csharp
using DeepSigma.DocumentReader;

IDocumentReader reader = DocumentReaderFactory.CreateDefault();

using DocumentSource source = DocumentSource.FromFile("invoice.pdf");
DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

Console.WriteLine(result.Kind);     // e.g. Pdf
Console.WriteLine(result.Quality);  // High / Medium / Low
Console.WriteLine(result.Text);     // the document's text projection
```

---

## Dependency injection

For ASP.NET Core / hosted apps, register everything once and inject `IDocumentReader`:

```csharp
builder.Services.AddDeepSigmaDocumentReaderDefaults();
```

…or compose exactly the readers you want (each format package adds its own extension):

```csharp
builder.Services
    .AddDeepSigmaDocumentReader()   // core + type detection
    .AddText()
    .AddJson()
    .AddCsv()
    .AddMarkdown()
    .AddOffice()                    // Word + Excel + PowerPoint
    .AddPdf()
    .AddHtml()
    .AddEmail();
```

A minimal Web API upload endpoint:

```csharp
app.MapPost("/extract", async (IFormFile file, IDocumentReader reader, CancellationToken ct) =>
{
    await using Stream stream = file.OpenReadStream();
    using DocumentSource source = DocumentSource.FromStream(stream, file.FileName, file.ContentType);

    DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default, ct);
    return Results.Json(new { result.Kind, result.Text, warnings = result.Warnings.Count });
});
```

---

## Reading from files, streams, and bytes

`DocumentSource` is the input. It owns the stream for `FromFile`/`FromBytes` (disposed with
the source) and borrows it for `FromStream` (the caller keeps ownership):

```csharp
using DocumentSource fromFile   = DocumentSource.FromFile("report.docx");
using DocumentSource fromBytes  = DocumentSource.FromBytes(bytes, "data.json", "application/json");
DocumentSource fromStream       = DocumentSource.FromStream(httpStream, "page.html", "text/html");
```

The stream does not need to be seekable — the reader buffers it safely (spilling large inputs
to a temp file) within the configured size limit.

---

## Working with the result

`DocumentReadResult` is the unified shape every reader produces:

```csharp
DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

// Flat text projection (useful for search / RAG).
string? text = result.Text;

// Heading hierarchy (Markdown, Word, HTML) — sections carry their body text.
foreach (DocumentSection section in result.Sections)
    Console.WriteLine($"{new string('#', section.Level)} {section.Title}\n{section.Text}");

// Tables (CSV, Excel, Word/PowerPoint, HTML, opt-in for PDF).
foreach (DocumentTable table in result.Tables)
    foreach (DocumentTableRow row in table.Rows)
        Console.WriteLine(string.Join(" | ", row.Cells.Select(c => c.Text)));

// Common metadata.
Console.WriteLine($"{result.Metadata.Title} by {result.Metadata.Author}");

// Non-fatal problems — readers prefer warnings over throwing.
foreach (DocumentWarning w in result.Warnings)
    Console.WriteLine($"[{w.Code}] {w.Message}");
```

### Format-specific features

Richer, per-format details hang off `Features`. Retrieve one with `GetFeature<T>()`:

```csharp
// Flattened JSONPath values:
if (result.GetFeature<JsonDocumentFeature>() is { } json)
    foreach (JsonPathValue v in json.Values)
        Console.WriteLine($"{v.Path} = {v.TextValue}");

// Spreadsheet sheets with typed cell values:
if (result.GetFeature<SpreadsheetDocumentFeature>() is { } book)
    foreach (SpreadsheetSheet sheet in book.Sheets)
        Console.WriteLine($"{sheet.Name}: {sheet.Table.Rows.Count} rows");

// Email envelope:
if (result.GetFeature<EmailDocumentFeature>() is { } email)
    Console.WriteLine($"From {email.From.FirstOrDefault()?.Address}: {email.Subject}");
```

Available features: `MarkdownDocumentFeature`, `JsonDocumentFeature`,
`SpreadsheetDocumentFeature`, `PresentationDocumentFeature`, `HtmlDocumentFeature`,
`EmailDocumentFeature`.

---

## Format-specific options

Global options (limits, what to extract) live on `DocumentReadOptions`. Per-format options
are attached through a type-keyed bag with `WithOptions<T>()`, so the contract never depends
on any one format:

```csharp
DocumentReadOptions options = DocumentReadOptions.Default
    .WithOptions(new JsonReadOptions { FlattenPaths = true, MaxDepth = 32 })
    .WithOptions(new CsvReadOptions { Delimiter = ";", HasHeaderRecord = true })
    .WithOptions(new PdfReadOptions { ExtractTables = true })   // best-effort PDF tables (opt-in)
    .WithOptions(new WordReadOptions { IncludeComments = true });

DocumentReadResult result = await reader.ReadAsync(source, options);
```

A reader reads its own options via `options.GetOptions<T>()`, falling back to that type's
defaults when none were supplied. Option types: `TextReadOptions`, `MarkdownReadOptions`,
`JsonReadOptions`, `CsvReadOptions`, `WordReadOptions`, `ExcelReadOptions`,
`PowerPointReadOptions`, `PdfReadOptions`, `HtmlReadOptions`, `EmailReadOptions`.

---

## Exporting a result

The `DeepSigma.DocumentReader.Export` package serializes a result to text, Markdown, or JSON.
Resolve an exporter by name:

```csharp
using DeepSigma.DocumentReader.Export;

IExporterResolver exporters = ExporterResolver.CreateDefault();   // or inject IExporterResolver
IDocumentResultExporter exporter = exporters.Resolve("markdown")!;

await using var output = File.Create("invoice.md");
await exporter.ExportAsync(result, output);
```

Markdown export is the richest projection (front-matter metadata + pipe tables) and is well
suited to search/summarization/RAG pipelines.

---

## Email attachments

The email reader exposes attachments, which can be read recursively as their own documents
(bounded by the normal size limit — there is no automatic recursion):

```csharp
DocumentReadResult email = await reader.ReadAsync(DocumentSource.FromFile("message.eml"), options);

foreach (DocumentAttachment attachment in email.Attachments)
{
    using DocumentSource attachmentSource = attachment.AsDocumentSource();
    DocumentReadResult inner = await reader.ReadAsync(attachmentSource, options);
    Console.WriteLine($"{attachment.FileName}: {inner.Kind}");
}
```

---

## Detecting a document's type

Detection runs automatically inside `ReadAsync`, but you can use it standalone — it combines
content-type, extension, magic bytes, OOXML container inspection, and content sniffing:

```csharp
IDocumentTypeDetector detector = CompositeDocumentTypeDetector.CreateDefault();

using DocumentSource source = DocumentSource.FromFile("unknown.bin");
DocumentTypeDetectionResult detection = await detector.DetectAsync(source);

Console.WriteLine($"{detection.Kind} ({detection.Confidence}%)");
foreach (DocumentTypeCandidate c in detection.Candidates)
    Console.WriteLine($"  {c.Kind} via {c.Signal} ({c.Confidence})");
```

---

## Security and resource limits

All input is treated as untrusted. Limits are configurable and applied before/while parsing:

```csharp
var options = new DocumentReadOptions
{
    MaxBytes = 50 * 1024 * 1024,        // reject sources larger than 50 MiB (default 256 MiB)
    Timeout = TimeSpan.FromSeconds(30), // overall read timeout
}
.WithOptions(new CsvReadOptions { MaxRows = 100_000 })
.WithOptions(new JsonReadOptions { MaxDepth = 64, MaxRecords = 1_000_000 });
```

Readers never execute macros, fetch remote content, or load external resources.

---

## Command-line tool (`dsread`)

```bash
dotnet tool install --global DeepSigma.DocumentReader.Cli

dsread detect  input.json
dsread extract input.md  --format json --output out.json
dsread inspect input.csv
dsread batch   ./inputs --output ./out --format markdown --recursive
```

`extract` accepts `--max-bytes`, `--max-rows`, `--max-depth`, and `--timeout`; `--strict`
makes warnings produce a non-zero exit code.

---

## Extending with a custom reader

Implement `IFormatDocumentReader` (or derive from `FormatDocumentReaderBase` in Core for the
buffering/limit plumbing) and register it on the builder:

```csharp
public sealed class XmlDocumentReader : FormatDocumentReaderBase
{
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.PlainText];

    protected override async Task<DocumentReadResult> ReadCoreAsync(
        DocumentReadContext context, CancellationToken cancellationToken)
    {
        string text = await TextContent.ReadAllTextAsync(context.Stream, cancellationToken);
        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.PlainText),
            Kind = DocumentKind.PlainText,
            Text = text,
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
        };
    }
}

// Registration:
builder.Services.AddDeepSigmaDocumentReader().AddReader<XmlDocumentReader>();
```

---

## Packages

Packages are grouped by *dependency tree*, so a consumer only pulls the dependencies it uses.

| Package | Purpose | External deps |
|---|---|---|
| `DeepSigma.DocumentReader.Abstractions` | Contracts only (interfaces, DTOs, enums, options, exceptions) | none |
| `DeepSigma.DocumentReader.Core` | Type detection, composite reader, stream/text helpers, DI, shared reader base/builders | DI abstractions |
| `DeepSigma.DocumentReader.Plaintext` | Text, Markdown, JSON/JSONL, CSV readers | Markdig, CsvHelper |
| `DeepSigma.DocumentReader.Office` | Word (DOCX), Excel (XLSX), PowerPoint (PPTX) readers | DocumentFormat.OpenXml, ClosedXML |
| `DeepSigma.DocumentReader.Pdf` | PDF reader (page text, page model, metadata; opt-in best-effort tables) | PdfPig |
| `DeepSigma.DocumentReader.Html` | HTML reader (text, sections, tables, links) | AngleSharp |
| `DeepSigma.DocumentReader.Email` | Email (.eml) reader (headers, bodies, attachments) | MimeKit |
| `DeepSigma.DocumentReader.Export` | Text / Markdown / JSON exporters | none |
| `DeepSigma.DocumentReader` | Convenience meta-package: factory + DI defaults | (the above) |
| `DeepSigma.DocumentReader.Cli` | `dsread` command-line tool | System.CommandLine |

The email reader reuses the HTML reader's text extraction (via the `IHtmlTextExtractor`
contract) when both are registered, without taking a hard dependency on it.

---

## Repository layout

```
src/        the packages above
tests/      unit tests, integration tests, and a shared Corpus/ of sample + golden files
samples/    runnable usage examples
docs/       design guidance
```

## Building and testing

```bash
dotnet build DeepSigma.DocumentReader.slnx
dotnet test  DeepSigma.DocumentReader.slnx
```

Golden-file expectations live next to their inputs under `tests/Corpus/`. To regenerate them
after an intentional output change, run the tests with `UPDATE_GOLDEN=1`.
