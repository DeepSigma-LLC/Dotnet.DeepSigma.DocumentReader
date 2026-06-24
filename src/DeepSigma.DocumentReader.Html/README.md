# DeepSigma.DocumentReader.Html

HTML reader for the DeepSigma.DocumentReader ecosystem, using AngleSharp: **readable text**,
a heading→section tree, tables, links/images, and the document title. **External resources
are never loaded.**

```bash
dotnet add package DeepSigma.DocumentReader.Html
```

## Register

```csharp
builder.Services.AddDeepSigmaDocumentReader().AddHtml();
```

`AddHtml()` also registers an `IHtmlTextExtractor`, which the
[email reader](https://www.nuget.org/packages/DeepSigma.DocumentReader.Email) reuses to
convert HTML message bodies to text when both packages are present.

Options: `HtmlReadOptions`. Feature: `HtmlDocumentFeature` (headings + links).

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
