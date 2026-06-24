# DeepSigma.DocumentReader.Export

Exporters that serialize a `DocumentReadResult` from the DeepSigma.DocumentReader ecosystem to
**plain text, Markdown, or JSON**. Depends only on the contracts package — never on a parser.

```bash
dotnet add package DeepSigma.DocumentReader.Export
```

## Use

```csharp
using DeepSigma.DocumentReader.Export;

IExporterResolver exporters = ExporterResolver.CreateDefault();  // or inject IExporterResolver
IDocumentResultExporter exporter = exporters.Resolve("markdown")!;

await using var output = File.Create("invoice.md");
await exporter.ExportAsync(result, output);
```

Markdown export is the richest projection (metadata front matter + pipe tables) and is well
suited to search, summarization, and RAG pipelines.

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
