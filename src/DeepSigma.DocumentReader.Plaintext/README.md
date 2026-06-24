# DeepSigma.DocumentReader.Plaintext

Lightweight, fully-managed readers for the DeepSigma.DocumentReader ecosystem: **plain text,
Markdown, JSON / JSON Lines, and CSV**. Uses Markdig and CsvHelper.

```bash
dotnet add package DeepSigma.DocumentReader.Plaintext
```

## Register

```csharp
builder.Services
    .AddDeepSigmaDocumentReader()
    .AddText()
    .AddMarkdown()
    .AddJson()
    .AddCsv();
```

## Highlights

- **Text** — encoding/BOM detection, UTF-8 → Latin-1 fallback, line-ending normalization.
- **Markdown** — heading→section tree, pipe tables, YAML front matter, code blocks, links.
- **JSON / JSONL** — flattened JSONPath values, per-line records, depth/record limits.
- **CSV** — delimiter detection, header handling, malformed-row warnings.

Options: `TextReadOptions`, `MarkdownReadOptions`, `JsonReadOptions`, `CsvReadOptions`
(attach via `DocumentReadOptions.WithOptions(...)`).

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
