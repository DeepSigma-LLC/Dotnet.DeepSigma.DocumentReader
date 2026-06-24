# DeepSigma.DocumentReader.Email

Email (`.eml`) reader for the DeepSigma.DocumentReader ecosystem, using MimeKit: headers,
sender/recipients, subject, date, text body (or HTML body converted to text), and
**attachments**. **Remote content is never fetched.**

```bash
dotnet add package DeepSigma.DocumentReader.Email
```

## Register

```csharp
builder.Services.AddDeepSigmaDocumentReader().AddEmail();
// add .AddHtml() too for richer HTML-body-to-text conversion
```

## Attachments

Attachments are exposed for recursive reading under your own limits:

```csharp
foreach (DocumentAttachment attachment in result.Attachments)
{
    using DocumentSource source = attachment.AsDocumentSource();
    DocumentReadResult inner = await reader.ReadAsync(source, options);
}
```

Options: `EmailReadOptions`. Feature: `EmailDocumentFeature` (envelope, bodies, headers).

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
