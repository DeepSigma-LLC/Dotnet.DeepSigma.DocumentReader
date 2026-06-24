namespace DeepSigma.DocumentReader;

/// <summary>
/// A format-specific extension to a <see cref="DocumentReadResult"/>. Implemented by
/// each format package (e.g. a Markdown feature exposing headings and front matter)
/// so the base result stays small.
/// </summary>
public interface IDocumentFeature
{
    /// <summary>A short, stable name identifying the feature (e.g. <c>Markdown</c>).</summary>
    string Name { get; }
}

/// <summary>Convenience accessors for retrieving features from a result.</summary>
public static class DocumentFeatureExtensions
{
    /// <summary>Returns the first feature of type <typeparamref name="T"/>, or <see langword="null"/>.</summary>
    /// <example>
    /// <code>
    /// if (result.GetFeature&lt;JsonDocumentFeature&gt;() is { } json)
    ///     foreach (JsonPathValue value in json.Values)
    ///         Console.WriteLine($"{value.Path} = {value.TextValue}");
    /// </code>
    /// </example>
    public static T? GetFeature<T>(this DocumentReadResult result)
        where T : class, IDocumentFeature
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Features.OfType<T>().FirstOrDefault();
    }

    /// <summary>Attempts to retrieve the first feature of type <typeparamref name="T"/>.</summary>
    public static bool TryGetFeature<T>(this DocumentReadResult result, out T feature)
        where T : class, IDocumentFeature
    {
        feature = result.GetFeature<T>()!;
        return feature is not null;
    }
}
