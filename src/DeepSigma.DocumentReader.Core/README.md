# DeepSigma.DocumentReader.Core

Orchestration for the DeepSigma.DocumentReader ecosystem: document **type detection**, the
**composite reader**, safe **stream/text** handling, shared reader base classes and builders,
and **dependency-injection** wiring. Depends only on the contracts package and
`Microsoft.Extensions.DependencyInjection.Abstractions`.

```bash
dotnet add package DeepSigma.DocumentReader.Core
```

What's inside:

- `CompositeDocumentReader` + `CompositeDocumentTypeDetector` (pluggable `IDetectionSignal`s,
  OOXML `[Content_Types].xml` inspection)
- `FormatDocumentReaderBase` / `DocumentReadContext` — the base every format reader derives from
- Shared helpers: `DocumentStreamBuffer`, `TextContent`, `DocumentTableBuilder`,
  `SectionTreeBuilder`, `SectionAccumulator`
- `AddDeepSigmaDocumentReader()` and the `IDocumentReaderBuilder` used by format packages

Format reader packages (Plaintext, Office, Pdf, Html, Email) build on this. Most apps should
install the [`DeepSigma.DocumentReader`](https://www.nuget.org/packages/DeepSigma.DocumentReader)
meta-package instead of wiring Core directly.

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
