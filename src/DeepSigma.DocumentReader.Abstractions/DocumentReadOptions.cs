using System.Collections.Immutable;

namespace DeepSigma.DocumentReader;

/// <summary>
/// Marker interface implemented by every format-specific options type. Each format
/// package defines its own options (e.g. <c>JsonReadOptions</c>) so that the contracts
/// package never has to reference any concrete format.
/// </summary>
public interface IFormatReadOptions;

/// <summary>
/// Options controlling a read operation. Global behavior and resource limits are
/// expressed as properties; format-specific options are carried in a type-keyed bag
/// accessed via <see cref="WithOptions{T}"/> and <see cref="GetOptions{T}"/>.
/// </summary>
/// <remarks>
/// Instances are immutable. Use <see cref="WithOptions{T}"/> to attach format options,
/// which returns a new instance.
/// </remarks>
public sealed class DocumentReadOptions
{
    /// <summary>The default maximum number of bytes a single document may occupy: 256 MiB.</summary>
    public const long DefaultMaxBytes = 256L * 1024 * 1024;

    private readonly ImmutableDictionary<Type, IFormatReadOptions> _formatOptions;

    /// <summary>Creates options with default values and no format-specific overrides.</summary>
    public DocumentReadOptions()
    {
        _formatOptions = ImmutableDictionary<Type, IFormatReadOptions>.Empty;
    }

    private DocumentReadOptions(DocumentReadOptions source, ImmutableDictionary<Type, IFormatReadOptions> formatOptions)
    {
        ExtractText = source.ExtractText;
        ExtractMetadata = source.ExtractMetadata;
        ExtractTables = source.ExtractTables;
        ExtractImages = source.ExtractImages;
        ExtractAttachments = source.ExtractAttachments;
        MaxPages = source.MaxPages;
        MaxBytes = source.MaxBytes;
        Timeout = source.Timeout;
        _formatOptions = formatOptions;
    }

    /// <summary>A shared instance carrying the default options.</summary>
    public static DocumentReadOptions Default { get; } = new();

    /// <summary>Whether to extract a text projection. Default <see langword="true"/>.</summary>
    public bool ExtractText { get; init; } = true;

    /// <summary>Whether to extract document metadata. Default <see langword="true"/>.</summary>
    public bool ExtractMetadata { get; init; } = true;

    /// <summary>Whether to extract tables. Default <see langword="true"/>.</summary>
    public bool ExtractTables { get; init; } = true;

    /// <summary>Whether to extract images. Default <see langword="false"/>.</summary>
    public bool ExtractImages { get; init; }

    /// <summary>Whether to extract attachments. Default <see langword="true"/>.</summary>
    public bool ExtractAttachments { get; init; } = true;

    /// <summary>Maximum number of pages to read, if applicable. <see langword="null"/> means unlimited.</summary>
    public int? MaxPages { get; init; }

    /// <summary>
    /// Maximum number of bytes the source may occupy before reading is aborted.
    /// Defaults to <see cref="DefaultMaxBytes"/>; set to <see langword="null"/> to disable.
    /// </summary>
    public long? MaxBytes { get; init; } = DefaultMaxBytes;

    /// <summary>An optional overall timeout for the read operation.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Returns a copy of these options with the supplied format-specific options attached,
    /// keyed by their runtime type.
    /// </summary>
    public DocumentReadOptions WithOptions<T>(T options)
        where T : class, IFormatReadOptions
    {
        ArgumentNullException.ThrowIfNull(options);
        return new DocumentReadOptions(this, _formatOptions.SetItem(typeof(T), options));
    }

    /// <summary>
    /// Returns the attached options of type <typeparamref name="T"/>, or a new instance
    /// with that type's defaults when none were attached.
    /// </summary>
    public T GetOptions<T>()
        where T : class, IFormatReadOptions, new()
        => _formatOptions.TryGetValue(typeof(T), out var value) ? (T)value : new T();
}
