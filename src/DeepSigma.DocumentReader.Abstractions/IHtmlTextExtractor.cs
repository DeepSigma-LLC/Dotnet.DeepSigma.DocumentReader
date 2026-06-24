namespace DeepSigma.DocumentReader;

/// <summary>
/// Converts an HTML fragment or document into readable plain text. Implemented by the HTML
/// package and consumed by other readers (e.g. the email reader for HTML bodies) so they can
/// reuse a real HTML parser when it is registered, without taking a hard dependency on it.
/// </summary>
public interface IHtmlTextExtractor
{
    /// <summary>Extracts readable plain text from the supplied HTML.</summary>
    string ExtractText(string html);
}
