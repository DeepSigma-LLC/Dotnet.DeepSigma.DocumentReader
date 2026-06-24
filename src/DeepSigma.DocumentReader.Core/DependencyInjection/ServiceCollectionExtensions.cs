using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using DeepSigma.DocumentReader.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeepSigma.DocumentReader;

/// <summary>Dependency-injection registration for the document reader core.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the type detector, composite reader, and default detection signals, and
    /// returns a builder for adding format readers (e.g. <c>.AddJson()</c>). Idempotent.
    /// </summary>
    public static IDocumentReaderBuilder AddDeepSigmaDocumentReader(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
        [
            ServiceDescriptor.Singleton<IDetectionSignal, ContentTypeSignal>(),
            ServiceDescriptor.Singleton<IDetectionSignal, ExtensionSignal>(),
            ServiceDescriptor.Singleton<IDetectionSignal, MagicBytesSignal>(),
            ServiceDescriptor.Singleton<IDetectionSignal, ContentSniffSignal>(),
        ]);

        services.TryAddSingleton<IDocumentTypeDetector, CompositeDocumentTypeDetector>();
        services.TryAddSingleton<IDocumentReaderProvider, DocumentReaderProvider>();
        services.TryAddSingleton<IDocumentReader, CompositeDocumentReader>();

        return new DocumentReaderBuilder(services);
    }
}
