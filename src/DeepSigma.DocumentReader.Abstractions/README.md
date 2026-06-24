# DeepSigma.DocumentReader.Abstractions

Dependency-light **contracts** for the DeepSigma.DocumentReader ecosystem: interfaces, result
DTOs, enums, options, and exceptions. Has **no external dependencies**, so any consumer or
plugin can reference it safely.

```bash
dotnet add package DeepSigma.DocumentReader.Abstractions
```

Key types:

- `IDocumentReader`, `IFormatDocumentReader`, `IDocumentTypeDetector`
- `DocumentSource`, `DocumentReadOptions` (+ the `IFormatReadOptions` typed-options bag)
- `DocumentReadResult` and its structural models (`DocumentPage`, `DocumentSection`,
  `DocumentTable`, `DocumentAttachment`, `DocumentMetadata`, `DocumentWarning`)
- `IDocumentFeature` (+ `GetFeature<T>()`), `IDocumentResultExporter`, `WarningCodes`

Reference this package when you want to depend on the contracts without pulling in any reader
implementation. For a ready-to-use reader, install
[`DeepSigma.DocumentReader`](https://www.nuget.org/packages/DeepSigma.DocumentReader) instead.

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
