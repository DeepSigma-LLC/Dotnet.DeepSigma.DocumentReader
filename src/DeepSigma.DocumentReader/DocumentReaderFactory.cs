using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.DocumentReader;

/// <summary>
/// Creates a ready-to-use <see cref="IDocumentReader"/> without requiring the caller to set
/// up dependency injection. For apps that already use DI, prefer
/// <see cref="DocumentReaderServiceCollectionExtensions.AddDeepSigmaDocumentReaderDefaults"/>.
/// </summary>
public static class DocumentReaderFactory
{
    // Built once and cached for the process. The backing ServiceProvider lives for the
    // application lifetime by design; this avoids leaking a new provider on every call.
    private static readonly Lazy<IDocumentReader> DefaultReader =
        new(BuildDefault, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Returns a process-wide shared reader with the default readers registered. For apps
    /// that manage their own lifetime/scopes, prefer
    /// <see cref="DocumentReaderServiceCollectionExtensions.AddDeepSigmaDocumentReaderDefaults"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// IDocumentReader reader = DocumentReaderFactory.CreateDefault();
    /// using DocumentSource source = DocumentSource.FromFile("invoice.pdf");
    /// DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);
    /// Console.WriteLine(result.Text);
    /// </code>
    /// </example>
    public static IDocumentReader CreateDefault() => DefaultReader.Value;

    private static IDocumentReader BuildDefault()
    {
        var services = new ServiceCollection();
        services.AddDeepSigmaDocumentReaderDefaults();
        return services.BuildServiceProvider().GetRequiredService<IDocumentReader>();
    }
}
