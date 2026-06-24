# DeepSigma.DocumentReader Initial Design Guidance

## 1. Purpose

`DeepSigma.DocumentReader` is a .NET package ecosystem for reading, parsing, and extracting usable information from common document formats.

The project is intended to support downstream applications that need to extract text, tables, metadata, attachments, and structural information from files such as:

- Plain text
- Markdown
- JSON / JSON Lines
- CSV
- Excel workbooks
- PDF files
- Word documents
- PowerPoint presentations
- Emails
- HTML documents
- OCR-supported scanned documents and images

The primary design goal is to give consuming applications one consistent API while hiding file-format-specific parsing complexity behind modular packages.

Example consumer usage:

```csharp
using DeepSigma.DocumentReader;

IDocumentReader reader = DocumentReaderFactory.CreateDefault();

DocumentReadResult result = await reader.ReadAsync(
    DocumentSource.FromFile("invoice.pdf"),
    DocumentReadOptions.Default,
    cancellationToken);

Console.WriteLine(result.Text);
```

The consuming application should not need to know whether the source file is a PDF, DOCX, XLSX, CSV, Markdown file, JSON file, email, or plain text file.

---

## 2. Core Design Principles

### 2.1 Separate document reading from document understanding

`DeepSigma.DocumentReader` should focus on extracting reliable, structured representations of documents.

It should produce:

- Text
- Tables
- Pages
- Sections
- Slides
- Sheets
- Attachments
- Metadata
- Source locations
- Warnings
- Confidence / quality information

It should not directly own downstream business interpretation such as:

- Invoice field extraction
- Contract clause extraction
- Entity extraction
- Summarization
- Classification
- Embedding generation
- Search indexing
- Validation workflows

Those capabilities can be built on top of this project later, but the document reader should remain general-purpose.

### 2.2 Use modular packages

Each format reader should live in its own package. Consumers should only need to reference the packages for formats they actually use.

### 2.3 Keep abstractions dependency-light

The abstractions package should contain interfaces, DTOs, enums, and small value objects only. It should not reference heavy parsing libraries.

### 2.4 Prefer partial results over failure

When a document can be partially parsed, return a result with warnings instead of throwing. Throw only when no useful result can be produced.

### 2.5 Treat all documents as untrusted input

Parsing documents is security-sensitive. The system should enforce limits, avoid executing embedded content, and prevent unsafe file or network access during parsing.

---

## 3. Package Structure

Recommended NuGet package structure:

```text
DeepSigma.DocumentReader
DeepSigma.DocumentReader.Abstractions
DeepSigma.DocumentReader.Core

DeepSigma.DocumentReader.Text
DeepSigma.DocumentReader.Markdown
DeepSigma.DocumentReader.Json
DeepSigma.DocumentReader.Csv
DeepSigma.DocumentReader.Excel
DeepSigma.DocumentReader.Pdf
DeepSigma.DocumentReader.Word
DeepSigma.DocumentReader.PowerPoint
DeepSigma.DocumentReader.Email
DeepSigma.DocumentReader.Html
DeepSigma.DocumentReader.Ocr

DeepSigma.DocumentReader.Export
DeepSigma.DocumentReader.All
DeepSigma.DocumentReader.Cli
```

### 3.1 `DeepSigma.DocumentReader`

Convenience package for common usage.

Recommended role:

- Reference `Abstractions`
- Reference `Core`
- Reference common readers
- Expose simple factory methods
- Provide default dependency injection registration

Example install:

```bash
dotnet add package DeepSigma.DocumentReader
```

### 3.2 `DeepSigma.DocumentReader.Abstractions`

Contracts-only package.

Contains:

- Interfaces
- Result models
- Option models
- Enums
- Exceptions
- Value objects

This package should be safe for any consuming app or plugin to reference.

### 3.3 `DeepSigma.DocumentReader.Core`

Orchestration package.

Contains:

- Composite reader
- Reader registry
- Document type detection
- MIME / extension / signature detection
- Common stream utilities
- Shared option handling
- Shared validation
- Common exception types

### 3.4 Format packages

Each format package owns one major file type or closely related family of file types.

Examples:

```text
DeepSigma.DocumentReader.Pdf
DeepSigma.DocumentReader.Word
DeepSigma.DocumentReader.Excel
DeepSigma.DocumentReader.Email
```

These packages should depend on `Abstractions` and usually `Core`, but not on each other unless there is a clear reason.

### 3.5 `DeepSigma.DocumentReader.All`

Optional meta-package that references all supported readers.

Useful for internal tools, prototyping, and applications that need broad support.

### 3.6 `DeepSigma.DocumentReader.Cli`

Command-line tool for debugging, batch extraction, and smoke testing.

Suggested command:

```bash
dsread extract input.pdf --format markdown --output output.md
dsread inspect input.xlsx --json
dsread detect input.eml
```

---

## 4. Repository Layout

Recommended repository layout:

```text
DeepSigma.DocumentReader/
  DeepSigma.DocumentReader.sln

  src/
    DeepSigma.DocumentReader/
    DeepSigma.DocumentReader.Abstractions/
    DeepSigma.DocumentReader.Core/
    DeepSigma.DocumentReader.Text/
    DeepSigma.DocumentReader.Markdown/
    DeepSigma.DocumentReader.Json/
    DeepSigma.DocumentReader.Csv/
    DeepSigma.DocumentReader.Excel/
    DeepSigma.DocumentReader.Pdf/
    DeepSigma.DocumentReader.Word/
    DeepSigma.DocumentReader.PowerPoint/
    DeepSigma.DocumentReader.Email/
    DeepSigma.DocumentReader.Html/
    DeepSigma.DocumentReader.Ocr/
    DeepSigma.DocumentReader.Export/
    DeepSigma.DocumentReader.All/
    DeepSigma.DocumentReader.Cli/

  tests/
    DeepSigma.DocumentReader.Core.Tests/
    DeepSigma.DocumentReader.Text.Tests/
    DeepSigma.DocumentReader.Markdown.Tests/
    DeepSigma.DocumentReader.Json.Tests/
    DeepSigma.DocumentReader.Csv.Tests/
    DeepSigma.DocumentReader.Excel.Tests/
    DeepSigma.DocumentReader.Pdf.Tests/
    DeepSigma.DocumentReader.Word.Tests/
    DeepSigma.DocumentReader.PowerPoint.Tests/
    DeepSigma.DocumentReader.Email.Tests/
    DeepSigma.DocumentReader.IntegrationTests/
    Corpus/

  samples/
    DeepSigma.DocumentReader.Samples.BasicExtraction/
    DeepSigma.DocumentReader.Samples.WebApiUpload/
    DeepSigma.DocumentReader.Samples.BatchWorker/
    DeepSigma.DocumentReader.Samples.RagPipeline/

  docs/
    architecture.md
    supported-formats.md
    security.md
    extension-guide.md
    roadmap.md
```

---

## 5. Target Frameworks

Recommended initial target:

```xml
<TargetFramework>net8.0</TargetFramework>
```

Optional multi-targeting:

```xml
<TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>
```

Use `netstandard2.0` only if broad compatibility is required. Otherwise, prefer modern .NET to reduce complexity.

---

## 6. Public Namespace Strategy

Use `DeepSigma.DocumentReader` as the root namespace.

Examples:

```csharp
namespace DeepSigma.DocumentReader;
namespace DeepSigma.DocumentReader.Abstractions;
namespace DeepSigma.DocumentReader.Core;
namespace DeepSigma.DocumentReader.Pdf;
namespace DeepSigma.DocumentReader.Word;
namespace DeepSigma.DocumentReader.PowerPoint;
```

Common consumer-facing types should be available from:

```csharp
using DeepSigma.DocumentReader;
```

---

## 7. Core Abstractions

### 7.1 Main reader interface

```csharp
public interface IDocumentReader
{
    bool CanRead(DocumentSource source);

    Task<DocumentReadResult> ReadAsync(
        DocumentSource source,
        DocumentReadOptions options,
        CancellationToken cancellationToken = default);
}
```

### 7.2 Format reader interface

Internally, it may be useful to distinguish the composite reader from format-specific readers.

```csharp
public interface IFormatDocumentReader : IDocumentReader
{
    IReadOnlyCollection<DocumentKind> SupportedKinds { get; }

    int GetConfidence(
        DocumentSource source,
        DocumentTypeDetectionResult detectionResult);
}
```

### 7.3 Document type detector

```csharp
public interface IDocumentTypeDetector
{
    ValueTask<DocumentTypeDetectionResult> DetectAsync(
        DocumentSource source,
        CancellationToken cancellationToken = default);
}
```

### 7.4 Reader provider

```csharp
public interface IDocumentReaderProvider
{
    IReadOnlyCollection<IDocumentReader> Readers { get; }
}
```

---

## 8. Document Source Model

Documents should be readable from files, streams, and byte arrays.

```csharp
public sealed class DocumentSource
{
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public Stream Stream { get; init; }

    public static DocumentSource FromFile(string path);

    public static DocumentSource FromStream(
        Stream stream,
        string? fileName = null,
        string? contentType = null);

    public static DocumentSource FromBytes(
        byte[] bytes,
        string? fileName = null,
        string? contentType = null);
}
```

Readers should not assume that the input stream is seekable. If seeking is required, the core package should provide a safe utility for buffering to memory or a temporary file based on configured size limits.

---

## 9. Document Type Detection

Document type detection should use several signals:

1. Explicit content type, if supplied
2. File extension
3. Magic bytes / file signature
4. Container inspection
5. Reader probing

Examples:

```text
.txt       text/plain
.md        text/markdown
.json      application/json
.jsonl     application/x-ndjson
.csv       text/csv
.pdf       application/pdf
.docx      application/vnd.openxmlformats-officedocument.wordprocessingml.document
.xlsx      application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
.pptx      application/vnd.openxmlformats-officedocument.presentationml.presentation
.eml       message/rfc822
.html      text/html
```

Office Open XML files such as DOCX, XLSX, and PPTX are ZIP containers. The detector should inspect `[Content_Types].xml` rather than relying only on extensions.

---

## 10. Document Kind Enum

```csharp
public enum DocumentKind
{
    Unknown,

    PlainText,
    Markdown,
    Json,
    JsonLines,
    Csv,

    Pdf,

    WordDocument,
    Spreadsheet,
    Presentation,

    Email,
    Html,

    Image,
    Archive
}
```

---

## 11. Read Options

Global options should cover common extraction behavior. Format-specific options should be composed into the global options object.

```csharp
public sealed class DocumentReadOptions
{
    public static DocumentReadOptions Default { get; } = new();

    public bool ExtractText { get; init; } = true;
    public bool ExtractMetadata { get; init; } = true;
    public bool ExtractTables { get; init; } = true;
    public bool ExtractImages { get; init; }
    public bool ExtractAttachments { get; init; } = true;

    public int? MaxPages { get; init; }
    public long? MaxBytes { get; init; }
    public TimeSpan? Timeout { get; init; }

    public TextReadOptions Text { get; init; } = new();
    public MarkdownReadOptions Markdown { get; init; } = new();
    public JsonReadOptions Json { get; init; } = new();
    public CsvReadOptions Csv { get; init; } = new();
    public ExcelReadOptions Excel { get; init; } = new();
    public PdfReadOptions Pdf { get; init; } = new();
    public WordReadOptions Word { get; init; } = new();
    public PowerPointReadOptions PowerPoint { get; init; } = new();
    public EmailReadOptions Email { get; init; } = new();
    public HtmlReadOptions Html { get; init; } = new();
    public OcrReadOptions Ocr { get; init; } = new();
}
```

---

## 12. Result Model

The result model should provide a simple text projection and structured extraction details.

```csharp
public sealed class DocumentReadResult
{
    public required DocumentSourceInfo Source { get; init; }

    public required DocumentKind Kind { get; init; }

    public string? Text { get; init; }

    public IReadOnlyList<DocumentPage> Pages { get; init; } = [];

    public IReadOnlyList<DocumentSection> Sections { get; init; } = [];

    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];

    public IReadOnlyList<DocumentImage> Images { get; init; } = [];

    public IReadOnlyList<DocumentAttachment> Attachments { get; init; } = [];

    public DocumentMetadata Metadata { get; init; } = new();

    public IReadOnlyList<DocumentWarning> Warnings { get; init; } = [];

    public ExtractionQuality Quality { get; init; } = ExtractionQuality.Unknown;

    public IReadOnlyList<IDocumentFeature> Features { get; init; } = [];
}
```

### 12.1 Feature model

A feature model allows format-specific information without bloating the base result.

```csharp
public interface IDocumentFeature
{
    string Name { get; }
}
```

Example features:

```text
MarkdownDocumentFeature
JsonDocumentFeature
SpreadsheetDocumentFeature
PresentationDocumentFeature
EmailDocumentFeature
PdfDocumentFeature
```

---

## 13. Source Locations

All extracted structures should support source location data when possible.

```csharp
public sealed record DocumentLocation(
    int? PageNumber = null,
    int? SlideNumber = null,
    int? SheetIndex = null,
    string? SheetName = null,
    int? Row = null,
    int? Column = null,
    string? SectionPath = null);
```

This helps downstream applications trace extracted values back to their source.

Example:

```text
Page 2, Table 1, Row 4, Column 3
Sheet "Revenue", Row 10, Column 5
Slide 6, Speaker Notes
Markdown heading path: /Overview/Architecture
```

---

## 14. Common Structural Models

### 14.1 Pages

```csharp
public sealed class DocumentPage
{
    public required int PageNumber { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<TextBlock> Blocks { get; init; } = [];
    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];
    public IReadOnlyList<DocumentImage> Images { get; init; } = [];
}
```

### 14.2 Sections

```csharp
public sealed class DocumentSection
{
    public string? Title { get; init; }
    public int Level { get; init; }
    public string? Text { get; init; }
    public DocumentLocation? Location { get; init; }
    public IReadOnlyList<DocumentSection> Children { get; init; } = [];
}
```

### 14.3 Tables

```csharp
public sealed class DocumentTable
{
    public string? Name { get; init; }
    public DocumentLocation? Location { get; init; }
    public IReadOnlyList<DocumentTableRow> Rows { get; init; } = [];
    public double? Confidence { get; init; }
}

public sealed class DocumentTableRow
{
    public int RowIndex { get; init; }
    public IReadOnlyList<DocumentTableCell> Cells { get; init; } = [];
}

public sealed class DocumentTableCell
{
    public int RowIndex { get; init; }
    public int ColumnIndex { get; init; }
    public string? Text { get; init; }
    public object? RawValue { get; init; }
    public DocumentLocation? Location { get; init; }
}
```

### 14.4 Attachments

```csharp
public sealed class DocumentAttachment
{
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public long? SizeBytes { get; init; }
    public DocumentLocation? Location { get; init; }

    public Stream OpenReadStream();
    public DocumentSource AsDocumentSource();
}
```

---

## 15. Reader Orchestration

The core package should provide a composite reader.

```csharp
public sealed class CompositeDocumentReader : IDocumentReader
{
    private readonly IReadOnlyList<IFormatDocumentReader> _readers;
    private readonly IDocumentTypeDetector _detector;

    public async Task<DocumentReadResult> ReadAsync(
        DocumentSource source,
        DocumentReadOptions options,
        CancellationToken cancellationToken = default)
    {
        var detected = await _detector.DetectAsync(source, cancellationToken);

        var reader = _readers
            .Where(r => r.CanRead(source))
            .OrderByDescending(r => r.GetConfidence(source, detected))
            .FirstOrDefault();

        if (reader is null)
        {
            throw new UnsupportedDocumentTypeException(source);
        }

        return await reader.ReadAsync(source, options, cancellationToken);
    }
}
```

---

## 16. Dependency Injection

Suggested registration style:

```csharp
using DeepSigma.DocumentReader;

builder.Services
    .AddDeepSigmaDocumentReader()
    .AddText()
    .AddMarkdown()
    .AddJson()
    .AddCsv()
    .AddExcel()
    .AddPdf()
    .AddWord()
    .AddPowerPoint()
    .AddEmail()
    .AddHtml();
```

Convenience registration:

```csharp
builder.Services.AddDeepSigmaDocumentReaderDefaults();
```

Suggested extension API:

```csharp
public static class ServiceCollectionExtensions
{
    public static IDocumentReaderBuilder AddDeepSigmaDocumentReader(
        this IServiceCollection services)
    {
        services.AddSingleton<IDocumentTypeDetector, CompositeDocumentTypeDetector>();
        services.AddSingleton<IDocumentReader, CompositeDocumentReader>();

        return new DocumentReaderBuilder(services);
    }
}
```

---

## 17. Format Reader Guidance

## 17.1 Text Reader

Package:

```text
DeepSigma.DocumentReader.Text
```

Supported formats:

```text
.txt
.log
.md fallback
.json fallback
.xml
```

Responsibilities:

- Encoding detection
- BOM handling
- Line ending normalization
- Large-file support
- Invalid character handling

Options:

```csharp
public sealed class TextReadOptions
{
    public Encoding? Encoding { get; init; }
    public bool DetectEncoding { get; init; } = true;
    public bool NormalizeLineEndings { get; init; } = true;
    public long? MaxCharacters { get; init; }
}
```

---

## 17.2 Markdown Reader

Package:

```text
DeepSigma.DocumentReader.Markdown
```

Supported formats:

```text
.md
.markdown
.mdx, optional
```

Primary outputs:

- Plain text
- Markdown text, optionally preserved
- Heading hierarchy
- Tables
- Code blocks
- Links
- Images
- YAML front matter

Suggested model:

```csharp
public sealed class MarkdownDocumentFeature : IDocumentFeature
{
    public string Name => "Markdown";
    public IReadOnlyList<DocumentHeading> Headings { get; init; } = [];
    public IReadOnlyList<DocumentCodeBlock> CodeBlocks { get; init; } = [];
    public IReadOnlyList<DocumentLink> Links { get; init; } = [];
    public IReadOnlyDictionary<string, object?> FrontMatter { get; init; } =
        new Dictionary<string, object?>();
}
```

Options:

```csharp
public sealed class MarkdownReadOptions
{
    public bool PreserveMarkdown { get; init; } = true;
    public bool ExtractFrontMatter { get; init; } = true;
    public bool ExtractCodeBlocks { get; init; } = true;
    public bool ExtractLinks { get; init; } = true;
    public bool TreatMdxAsMarkdown { get; init; } = false;
}
```

Recommended behavior:

- Parse Markdown tables into `DocumentTable`.
- Treat code blocks as code, not normal prose.
- Convert headings into `DocumentSection` objects.
- Preserve original Markdown when requested.
- Extract readable plain text by default.

---

## 17.3 JSON Reader

Package:

```text
DeepSigma.DocumentReader.Json
```

Supported formats:

```text
.json
.jsonl
.ndjson
.geojson, optional
```

Primary outputs:

- Raw JSON text
- Pretty-printed JSON
- Flattened key-value paths
- Structured object tree
- Records for JSONL / NDJSON

JSON should be treated as semi-structured data, not merely text.

Suggested model:

```csharp
public sealed class JsonDocumentFeature : IDocumentFeature
{
    public string Name => "Json";
    public JsonValueKind RootKind { get; init; }
    public IReadOnlyList<JsonPathValue> Values { get; init; } = [];
    public IReadOnlyList<JsonRecord> Records { get; init; } = [];
}

public sealed record JsonPathValue(
    string Path,
    string? TextValue,
    object? RawValue,
    JsonValueKind ValueKind);
```

Example flattened paths:

```text
$.customer.name = "Acme Corp"
$.invoice.total = 1234.56
$.items[0].sku = "ABC-123"
```

Options:

```csharp
public sealed class JsonReadOptions
{
    public bool PrettyPrint { get; init; } = true;
    public bool FlattenPaths { get; init; } = true;
    public bool PreserveRawJson { get; init; } = true;
    public bool TreatJsonLinesAsRecords { get; init; } = true;
    public int? MaxDepth { get; init; } = 128;
    public int? MaxRecords { get; init; }
}
```

Implementation guidance:

- Use `System.Text.Json` by default.
- Consider `Newtonsoft.Json` only if extensive JSONPath behavior is required.
- Enforce max depth and max record limits.

---

## 17.4 CSV Reader

Package:

```text
DeepSigma.DocumentReader.Csv
```

Primary outputs:

- Plain text projection
- Structured table
- Header information
- Row and column values

Options:

```csharp
public sealed class CsvReadOptions
{
    public bool HasHeaderRecord { get; init; } = true;
    public string? Delimiter { get; init; }
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
    public int? MaxRows { get; init; }
}
```

Implementation guidance:

- Use a proven CSV parser rather than writing custom parsing logic.
- Support delimiter detection when delimiter is not specified.
- Return malformed row warnings instead of failing the whole document when possible.

---

## 17.5 Excel Reader

Package:

```text
DeepSigma.DocumentReader.Excel
```

Supported formats:

```text
.xlsx
.xlsm
.xls
.xlsb, optional depending on library support
```

Primary outputs:

- Workbook metadata
- Sheets
- Rows
- Cells
- Tables
- Cell addresses
- Formula text, optional
- Formatted values, optional
- Raw values, optional

Suggested model:

```csharp
public sealed class SpreadsheetDocumentFeature : IDocumentFeature
{
    public string Name => "Spreadsheet";
    public IReadOnlyList<SpreadsheetSheet> Sheets { get; init; } = [];
}

public sealed class SpreadsheetSheet
{
    public required int SheetIndex { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];
}

public sealed class SpreadsheetCell
{
    public required int RowIndex { get; init; }
    public required int ColumnIndex { get; init; }
    public string? Address { get; init; }
    public object? RawValue { get; init; }
    public string? Text { get; init; }
    public string? Formula { get; init; }
}
```

Important caution:

- Most libraries read cached formula results; they do not recalculate formulas.
- Formula evaluation should not be assumed unless explicitly implemented.

---

## 17.6 PDF Reader

Package:

```text
DeepSigma.DocumentReader.Pdf
```

Primary outputs:

- Page text
- Page model
- Text blocks
- Metadata
- Images, optional
- Tables, best-effort
- OCR fallback, optional

Options:

```csharp
public sealed class PdfReadOptions
{
    public bool ExtractText { get; init; } = true;
    public bool ExtractImages { get; init; }
    public bool ExtractTables { get; init; }
    public bool PreserveLayout { get; init; }
    public bool UseOcrFallback { get; init; }
    public int? MaxPages { get; init; }
}
```

Guidance:

- PDF text extraction should be page-based.
- PDF table extraction should include a confidence score.
- Scanned PDFs should produce a clear warning when no text layer exists.
- OCR should remain optional.
- Do not promise perfect table extraction from arbitrary PDFs.

Suggested PDF table extraction approach:

1. Extract words with coordinates.
2. Group words into lines.
3. Group lines into candidate rows.
4. Infer columns based on x-coordinate alignment.
5. Create `DocumentTable` with confidence score.
6. Add warnings when confidence is low.

---

## 17.7 Word Reader

Package:

```text
DeepSigma.DocumentReader.Word
```

Supported formats:

```text
.docx
.docm, read only; never execute macros
.doc, later / conversion-based
.rtf, optional
.odt, optional
```

Primary outputs:

- Paragraphs
- Headings
- Sections
- Tables
- Headers / footers, optional
- Footnotes / endnotes, optional
- Comments, optional
- Tracked changes, optional
- Embedded images, optional

Options:

```csharp
public sealed class WordReadOptions
{
    public bool IncludeHeadersAndFooters { get; init; }
    public bool IncludeFootnotes { get; init; }
    public bool IncludeComments { get; init; }
    public bool IncludeDeletedText { get; init; }
    public bool ExtractTables { get; init; } = true;
}
```

Guidance:

- Start with DOCX.
- Treat legacy DOC as a separate problem.
- Avoid Office COM automation for server-side parsing.
- Never execute macros.

---

## 17.8 PowerPoint Reader

Package:

```text
DeepSigma.DocumentReader.PowerPoint
```

Supported formats:

```text
.pptx
.pptm, read only; never execute macros
.ppt, later / conversion-based
```

Primary outputs:

- Slides
- Slide text
- Speaker notes
- Tables
- Images
- Chart metadata, optional
- Embedded object metadata, optional
- Presentation metadata

Suggested model:

```csharp
public sealed class PresentationDocumentFeature : IDocumentFeature
{
    public string Name => "Presentation";
    public IReadOnlyList<PresentationSlide> Slides { get; init; } = [];
}

public sealed class PresentationSlide
{
    public required int SlideNumber { get; init; }
    public string? Title { get; init; }
    public string? Text { get; init; }
    public string? SpeakerNotes { get; init; }

    public IReadOnlyList<TextBlock> TextBlocks { get; init; } = [];
    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];
    public IReadOnlyList<DocumentImage> Images { get; init; } = [];
}
```

Options:

```csharp
public sealed class PowerPointReadOptions
{
    public bool ExtractSlideText { get; init; } = true;
    public bool ExtractSpeakerNotes { get; init; } = true;
    public bool ExtractTables { get; init; } = true;
    public bool ExtractImages { get; init; }
    public bool ExtractCharts { get; init; } = false;
    public bool IncludeHiddenSlides { get; init; } = false;
}
```

Recommended Markdown projection:

```markdown
# Presentation: Quarterly Review

## Slide 1: Executive Summary

Revenue increased year over year.

### Speaker Notes

Discuss regional performance.

## Slide 2: Financials

| Metric | Q1 | Q2 |
|---|---:|---:|
| Revenue | 100 | 120 |
```

---

## 17.9 Email Reader

Package:

```text
DeepSigma.DocumentReader.Email
```

Supported formats:

```text
.eml
.msg, optional / later
.mbox, optional / later
```

Primary outputs:

- From
- To
- Cc
- Bcc
- Subject
- Date
- Plain text body
- HTML body converted to text
- Attachments
- Inline images
- Message headers

Suggested model:

```csharp
public sealed class EmailDocumentFeature : IDocumentFeature
{
    public string Name => "Email";
    public string? Subject { get; init; }
    public IReadOnlyList<EmailAddress> From { get; init; } = [];
    public IReadOnlyList<EmailAddress> To { get; init; } = [];
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public DateTimeOffset? Date { get; init; }
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();
}
```

Attachment behavior:

```csharp
foreach (var attachment in result.Attachments)
{
    DocumentReadResult attachmentResult = await reader.ReadAsync(
        attachment.AsDocumentSource(),
        options,
        cancellationToken);
}
```

Guidance:

- Use a MIME parser for `.eml`.
- Keep email parsing separate from mail server access.
- Do not recursively parse attachments without depth and size limits.

---

## 17.10 HTML Reader

Package:

```text
DeepSigma.DocumentReader.Html
```

Supported formats:

```text
.html
.htm
```

Primary outputs:

- Readable text
- Headings
- Tables
- Links
- Images
- Metadata

Guidance:

- Disable external resource loading.
- Parse tables into `DocumentTable`.
- Convert HTML headings into sections.
- Use this reader for email HTML bodies when available.

---

## 17.11 OCR Reader

Package:

```text
DeepSigma.DocumentReader.Ocr
```

Purpose:

- OCR scanned PDFs
- OCR image-only documents
- OCR embedded images when requested

Suggested abstraction:

```csharp
public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(
        Stream image,
        OcrOptions options,
        CancellationToken cancellationToken = default);
}
```

Options:

```csharp
public sealed class OcrReadOptions
{
    public bool Enabled { get; init; }
    public string Language { get; init; } = "eng";
    public int? Dpi { get; init; }
    public double? MinConfidence { get; init; }
}
```

Guidance:

- Keep OCR optional.
- Avoid native OCR dependencies in the core package.
- Return confidence scores.
- Clearly warn when OCR fallback was used.

---

## 18. Export Package

Package:

```text
DeepSigma.DocumentReader.Export
```

Supported output formats:

- Plain text
- Markdown
- JSON
- NDJSON
- HTML, optional

Suggested interface:

```csharp
public interface IDocumentResultExporter
{
    string ContentType { get; }

    Task ExportAsync(
        DocumentReadResult result,
        Stream output,
        CancellationToken cancellationToken = default);
}
```

Markdown export is especially useful for search, summarization, and LLM/RAG pipelines because it preserves headings, slide boundaries, table structure, and page markers.

---

## 19. Streaming Support

The simple API should be the default:

```csharp
Task<DocumentReadResult> ReadAsync(...)
```

For large files, add streaming APIs later:

```csharp
IAsyncEnumerable<DocumentReadEvent> ReadStreamingAsync(
    DocumentSource source,
    DocumentReadOptions options,
    CancellationToken cancellationToken = default);
```

Example event types:

```text
DocumentStarted
PageExtracted
SlideExtracted
SheetExtracted
TableExtracted
AttachmentFound
WarningRaised
DocumentCompleted
```

CSV and Excel may also benefit from row streaming:

```csharp
IAsyncEnumerable<DocumentTableRow> ReadRowsAsync(
    DocumentSource source,
    DocumentReadOptions options,
    CancellationToken cancellationToken = default);
```

---

## 20. Error and Warning Strategy

Use warnings for partial extraction problems.

```csharp
public sealed class DocumentWarning
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public DocumentLocation? Location { get; init; }
    public Exception? Exception { get; init; }
}
```

Example warning codes:

```text
Pdf.TextLayerMissing
Pdf.OcrFallbackUsed
Pdf.LowConfidenceTableExtraction
Excel.FormulaNotCalculated
Word.UnsupportedEmbeddedObject
PowerPoint.HiddenSlideSkipped
Email.AttachmentSkipped
Csv.MalformedRow
Json.MaxDepthExceeded
Document.PasswordProtected
Document.Encrypted
Document.UnsupportedEncoding
Document.SizeLimitExceeded
Document.TimeoutExceeded
```

Throw exceptions for unrecoverable cases:

- Unsupported document type
- Password-protected file with no password support
- File exceeds configured maximum size before parsing can begin
- Invalid source stream
- Total parsing timeout

---

## 21. Security Guidance

All input documents should be treated as untrusted.

Required safeguards:

- Maximum file size
- Maximum page count
- Maximum worksheet rows
- Maximum CSV rows
- Maximum JSON depth
- Maximum attachment depth
- Maximum total extracted attachment size
- Timeout support
- Cancellation token support
- Zip bomb protection
- Path traversal protection for embedded files
- No macro execution
- No external resource loading by default
- No network access during parsing
- Safe temporary file handling
- Secure cleanup of temporary files

Format-specific notes:

- Office files: never execute macros.
- HTML: do not fetch external scripts, images, CSS, or other resources.
- PDF: do not execute JavaScript or embedded actions.
- Email: do not fetch remote images or links.
- Archives: protect against decompression bombs and unsafe paths.

For high-risk environments, consider an optional worker-process isolation model where parsing runs in a separate process with strict resource limits.

---

## 22. Testing Strategy

Create a shared document corpus.

```text
tests/
  Corpus/
    Text/
    Markdown/
    Json/
    Csv/
    Excel/
    Pdf/
    Word/
    PowerPoint/
    Email/
    Html/
    Malformed/
    Encrypted/
    Large/
```

Test categories:

- Golden-file output tests
- Metadata extraction tests
- Table extraction tests
- Encoding tests
- Malformed input tests
- Password / encryption tests
- Large file performance tests
- Cross-platform tests
- Fuzz tests
- Attachment recursion tests
- Security limit tests

Golden-file example:

```text
invoice.pdf -> invoice.expected.md
invoice.pdf -> invoice.expected.json
quarterly-review.pptx -> quarterly-review.expected.md
sample-email.eml -> sample-email.expected.json
```

Normalize volatile values before snapshot comparison:

- Absolute paths
- Timestamps
- GUIDs
- Temporary file names
- Library-specific ordering differences

---

## 23. Recommended Initial Roadmap

### Phase 1: Core foundation and text-like formats

Build:

```text
DeepSigma.DocumentReader.Abstractions
DeepSigma.DocumentReader.Core
DeepSigma.DocumentReader.Text
DeepSigma.DocumentReader.Markdown
DeepSigma.DocumentReader.Json
DeepSigma.DocumentReader.Csv
DeepSigma.DocumentReader.Export
DeepSigma.DocumentReader.Cli
```

Deliver:

- Unified result model
- Type detection
- Plain text extraction
- Markdown extraction
- JSON extraction
- CSV extraction
- Markdown / JSON export
- CLI smoke testing
- Initial test corpus

### Phase 2: Office formats

Build:

```text
DeepSigma.DocumentReader.Word
DeepSigma.DocumentReader.Excel
DeepSigma.DocumentReader.PowerPoint
```

Deliver:

- DOCX text and tables
- XLSX sheets, rows, cells, and tables
- PPTX slides, text, speaker notes, and tables
- Office Open XML type detection
- Office metadata extraction

### Phase 3: PDF

Build:

```text
DeepSigma.DocumentReader.Pdf
```

Deliver:

- Digital PDF text extraction
- Page model
- Basic metadata
- Optional image extraction
- Best-effort table extraction
- Warnings for scanned PDFs

### Phase 4: Email and HTML

Build:

```text
DeepSigma.DocumentReader.Email
DeepSigma.DocumentReader.Html
```

Deliver:

- EML parsing
- Plain body extraction
- HTML body extraction
- Attachment extraction
- Recursive attachment reading with limits

### Phase 5: OCR and advanced formats

Build:

```text
DeepSigma.DocumentReader.Ocr
DeepSigma.DocumentReader.Images
DeepSigma.DocumentReader.Archives
```

Deliver:

- Scanned PDF fallback
- Image OCR
- Confidence scoring
- Optional archive recursion

---

## 24. Suggested External Library Strategy

Keep all external libraries behind DeepSigma abstractions.

Potential library categories:

| Format | Library Strategy |
|---|---|
| Text | Built-in .NET streams and encoding support |
| Markdown | Markdown parser such as Markdig |
| JSON | `System.Text.Json` by default |
| CSV | Established CSV parser such as CsvHelper |
| Excel | ExcelDataReader, ClosedXML, or Open XML SDK depending on needs |
| Word DOCX | Open XML SDK |
| PowerPoint PPTX | Open XML SDK |
| PDF | .NET PDF text extraction library such as PdfPig |
| Email EML | MIME parser such as MimeKit |
| HTML | HTML parser such as AngleSharp or HtmlAgilityPack |
| OCR | Optional Tesseract wrapper or pluggable OCR engine |

Do not leak external library types into public contracts unless there is a deliberate reason.

---

## 25. Sample Consumer APIs

### 25.1 Simple file extraction

```csharp
IDocumentReader reader = DocumentReaderFactory.CreateDefault();

DocumentReadResult result = await reader.ReadAsync(
    DocumentSource.FromFile("contract.docx"),
    DocumentReadOptions.Default,
    cancellationToken);

Console.WriteLine(result.Text);
```

### 25.2 Web API upload

```csharp
app.MapPost("/extract", async (
    IFormFile file,
    IDocumentReader reader,
    CancellationToken cancellationToken) =>
{
    await using var stream = file.OpenReadStream();

    var result = await reader.ReadAsync(
        DocumentSource.FromStream(stream, file.FileName, file.ContentType),
        DocumentReadOptions.Default,
        cancellationToken);

    return Results.Json(result);
});
```

### 25.3 Batch extraction

```csharp
foreach (var path in Directory.EnumerateFiles(inputFolder))
{
    var result = await reader.ReadAsync(
        DocumentSource.FromFile(path),
        options,
        cancellationToken);

    await exporter.ExportAsync(result, outputStream, cancellationToken);
}
```

### 25.4 Recursive email attachment extraction

```csharp
DocumentReadResult email = await reader.ReadAsync(
    DocumentSource.FromFile("message.eml"),
    options,
    cancellationToken);

foreach (var attachment in email.Attachments)
{
    DocumentReadResult attachmentResult = await reader.ReadAsync(
        attachment.AsDocumentSource(),
        options,
        cancellationToken);
}
```

---

## 26. RAG and Search Pipeline Support

Add chunking support after the core readers are stable.

```csharp
public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunk> Chunk(
        DocumentReadResult document,
        ChunkingOptions options);
}
```

Suggested chunk model:

```csharp
public sealed class DocumentChunk
{
    public required string Text { get; init; }
    public required DocumentLocation Location { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
```

Metadata should include:

- Source file name
- Document kind
- Page number
- Slide number
- Sheet name
- Section path
- Table index
- Row / column information when applicable

---

## 27. Extension Model

Allow third parties or future DeepSigma packages to add readers.

```csharp
public interface IDocumentReaderPlugin
{
    string Name { get; }
    Version Version { get; }

    void Register(IDocumentReaderBuilder builder);
}
```

Builder:

```csharp
public interface IDocumentReaderBuilder
{
    IServiceCollection Services { get; }

    IDocumentReaderBuilder AddReader<TReader>()
        where TReader : class, IFormatDocumentReader;
}
```

Potential future packages:

```text
DeepSigma.DocumentReader.Images
DeepSigma.DocumentReader.Archives
DeepSigma.DocumentReader.Xml
DeepSigma.DocumentReader.Yaml
DeepSigma.DocumentReader.Rtf
DeepSigma.DocumentReader.OpenDocument
DeepSigma.DocumentReader.Visio
```

---

## 28. CLI Guidance

Package:

```text
DeepSigma.DocumentReader.Cli
```

Suggested command:

```text
dsread
```

Example commands:

```bash
dsread detect input.pdf

dsread extract input.pdf --format text

dsread extract input.docx --format markdown --output output.md

dsread extract workbook.xlsx --format json --output workbook.json

dsread inspect message.eml --json

dsread batch ./input --output ./output --format markdown
```

The CLI should be used for:

- Manual smoke testing
- Debugging parser behavior
- Creating expected test outputs
- Batch extraction
- Reproducing bugs

---

## 29. Documentation Plan

Recommended docs:

```text
docs/architecture.md
docs/supported-formats.md
docs/security.md
docs/extension-guide.md
docs/roadmap.md
docs/reader-options.md
docs/result-model.md
docs/testing.md
```

Each format package should include:

- Supported extensions
- Supported features
- Known limitations
- Security notes
- Example usage
- Option reference

---

## 30. Initial Definition of Done

For the first usable release, target the following:

- Core abstractions are stable enough for format readers.
- Type detection works for text, Markdown, JSON, CSV, DOCX, XLSX, PPTX, PDF, EML, and HTML.
- Text, Markdown, JSON, and CSV readers are implemented.
- Export to plain text, Markdown, and JSON is available.
- CLI can detect and extract supported Phase 1 formats.
- Test corpus exists.
- Security limits are configurable.
- DI registration works.
- NuGet packaging metadata is configured.
- Public API avoids leaking third-party parser types.

---

## 31. Open Design Questions

The following should be decided before the first stable release:

1. Should `DeepSigma.DocumentReader` be a convenience meta-package or the core implementation package?
2. Should `DocumentReadResult.Features` use interfaces, generic typed accessors, or a dictionary-based model?
3. How should password-protected documents be handled?
4. Should OCR be invoked automatically or only when explicitly enabled?
5. Should old binary Office formats such as `.doc`, `.xls`, and `.ppt` be supported directly, through conversion, or not in v1?
6. Should archive extraction be part of the core roadmap or a later package?
7. Should the library include built-in chunking for RAG pipelines in v1, or leave that for a companion package?
8. What should the default maximum file size, page count, row count, and attachment depth be?
9. Should Markdown export be considered the canonical normalized document representation?
10. Should structured data extraction from JSON and spreadsheets expose raw typed values or string-only normalized values?

---

## 32. Recommended Near-Term Implementation Order

Recommended first coding sequence:

1. Create solution and repository structure.
2. Implement `Abstractions`.
3. Implement `Core` stream handling and type detection.
4. Implement `Text` reader.
5. Implement `Json` reader.
6. Implement `Csv` reader.
7. Implement `Markdown` reader.
8. Implement exporters for text, Markdown, and JSON.
9. Implement CLI `detect` and `extract` commands.
10. Add test corpus and golden-file tests.
11. Add Word, Excel, and PowerPoint readers.
12. Add PDF reader.
13. Add Email and HTML readers.
14. Add OCR integration.

This order gives the project a usable foundation quickly while leaving the most complex formats until the core model has stabilized.
