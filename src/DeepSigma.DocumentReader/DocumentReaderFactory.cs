using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.DocumentReader;

/// <summary>
/// Creates a ready-to-use <see cref="IDocumentReader"/> without requiring the caller to set
/// up dependency injection. For apps that already use DI, prefer
/// <see cref="DocumentReaderServiceCollectionExtensions.AddDeepSigmaDocumentReaderDefaults"/>.
/// </summary>
public static class DocumentReaderFactory
{
    /// <summary>Builds a reader with the default text-family readers registered.</summary>
    public static IDocumentReader CreateDefault()
    {
        var services = new ServiceCollection();
        services.AddDeepSigmaDocumentReaderDefaults();
        return services.BuildServiceProvider().GetRequiredService<IDocumentReader>();
    }
}
