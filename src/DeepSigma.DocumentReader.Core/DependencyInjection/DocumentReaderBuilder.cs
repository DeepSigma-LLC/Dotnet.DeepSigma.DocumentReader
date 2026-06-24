using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.DocumentReader.Core.DependencyInjection;

/// <summary>Default <see cref="IDocumentReaderBuilder"/> backed by a service collection.</summary>
internal sealed class DocumentReaderBuilder(IServiceCollection services) : IDocumentReaderBuilder
{
    public IServiceCollection Services { get; } = services;

    public IDocumentReaderBuilder AddReader<TReader>()
        where TReader : class, IFormatDocumentReader
    {
        Services.AddSingleton<IFormatDocumentReader, TReader>();
        return this;
    }
}

/// <summary>Default <see cref="IDocumentReaderProvider"/> over the registered readers.</summary>
internal sealed class DocumentReaderProvider(IEnumerable<IFormatDocumentReader> readers) : IDocumentReaderProvider
{
    public IReadOnlyCollection<IFormatDocumentReader> Readers { get; } = readers.ToList();
}
