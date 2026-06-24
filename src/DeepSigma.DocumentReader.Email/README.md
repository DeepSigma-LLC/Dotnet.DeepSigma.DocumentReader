# DeepSigma.DocumentReader.Email

Email reader for the DeepSigma.DocumentReader ecosystem: **MIME (`.eml`)** via MimeKit and
**Outlook (`.msg`)** via MsgReader. Extracts headers, sender/recipients, subject, date, text
body (or HTML body converted to text), and **attachments**. Both formats produce the same
`DocumentKind.Email` result. **Remote content is never fetched.**

```bash
dotnet add package DeepSigma.DocumentReader.Email
```

## Register

```csharp
builder.Services.AddDeepSigmaDocumentReader().AddEmail();
// registers both the .eml (MimeKit) and .msg (MsgReader) readers
// add .AddHtml() too for richer HTML-body-to-text conversion
```

The right reader is chosen automatically from the content: OLE2 compound files (`.msg`) go to
the Outlook reader, plain MIME (`.eml`) to the MimeKit reader.

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
