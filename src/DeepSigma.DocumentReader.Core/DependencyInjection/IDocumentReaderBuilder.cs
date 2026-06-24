using Microsoft.Extensions.DependencyInjection;

namespace DeepSigma.DocumentReader;

/// <summary>
/// A fluent builder for registering document readers. Format packages add extension methods
/// (e.g. <c>AddJson()</c>) on this type. Defined in Core (not Abstractions) because it
/// depends on <see cref="IServiceCollection"/>.
/// </summary>
public interface IDocumentReaderBuilder
{
    /// <summary>The underlying service collection.</summary>
    IServiceCollection Services { get; }

    /// <summary>Registers a format reader implementation.</summary>
    IDocumentReaderBuilder AddReader<TReader>()
        where TReader : class, IFormatDocumentReader;
}

/// <summary>
/// A plug-in that registers one or more readers with a builder, enabling discovery-based
/// composition of reader packages.
/// </summary>
public interface IDocumentReaderPlugin
{
    /// <summary>The plug-in name.</summary>
    string Name { get; }

    /// <summary>The plug-in version.</summary>
    Version Version { get; }

    /// <summary>Registers the plug-in's readers with the builder.</summary>
    void Register(IDocumentReaderBuilder builder);
}
