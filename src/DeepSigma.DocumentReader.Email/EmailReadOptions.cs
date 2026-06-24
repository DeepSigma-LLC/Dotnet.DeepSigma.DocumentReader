namespace DeepSigma.DocumentReader;

/// <summary>Options for the email (.eml) reader.</summary>
public sealed class EmailReadOptions : IFormatReadOptions
{
    /// <summary>
    /// When both a plain-text and an HTML body are present, prefer text derived from the
    /// HTML body for the result's text projection. Default <see langword="false"/>.
    /// </summary>
    public bool PreferHtmlBody { get; init; }
}

/// <summary>An email address with an optional display name.</summary>
/// <param name="Name">The display name, if any.</param>
/// <param name="Address">The email address.</param>
public sealed record EmailAddress(string? Name, string Address);

/// <summary>Format-specific email details attached to a read result.</summary>
public sealed class EmailDocumentFeature : IDocumentFeature
{
    /// <inheritdoc />
    public string Name => "Email";

    /// <summary>The subject line.</summary>
    public string? Subject { get; init; }

    /// <summary>The sender addresses.</summary>
    public IReadOnlyList<EmailAddress> From { get; init; } = [];

    /// <summary>The primary recipient addresses.</summary>
    public IReadOnlyList<EmailAddress> To { get; init; } = [];

    /// <summary>The carbon-copy recipient addresses.</summary>
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];

    /// <summary>The sent date, if present.</summary>
    public DateTimeOffset? Date { get; init; }

    /// <summary>The plain-text body, if present.</summary>
    public string? TextBody { get; init; }

    /// <summary>The HTML body, if present.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The message headers (last value wins for duplicates).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
