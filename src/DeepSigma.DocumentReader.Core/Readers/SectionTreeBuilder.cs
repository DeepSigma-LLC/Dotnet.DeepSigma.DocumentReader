namespace DeepSigma.DocumentReader.Core.Readers;

/// <summary>A flat heading entry consumed by <see cref="SectionTreeBuilder"/>.</summary>
/// <param name="Level">Heading level (1 = top level).</param>
/// <param name="Title">Heading text.</param>
/// <param name="Text">Body text owned directly by the section, if any.</param>
/// <param name="Location">Where the heading originated, if known.</param>
public readonly record struct HeadingEntry(int Level, string? Title, string? Text = null, DocumentLocation? Location = null);

/// <summary>
/// Builds a nested <see cref="DocumentSection"/> tree from a flat, in-order sequence of
/// headings, nesting by level and computing each section's slash-delimited path.
/// </summary>
public static class SectionTreeBuilder
{
    private sealed class Node(HeadingEntry entry)
    {
        public HeadingEntry Entry { get; } = entry;
        public List<Node> Children { get; } = [];
    }

    /// <summary>Builds the root sections from the supplied headings.</summary>
    public static IReadOnlyList<DocumentSection> Build(IEnumerable<HeadingEntry> headings)
    {
        ArgumentNullException.ThrowIfNull(headings);

        var root = new Node(new HeadingEntry(0, null));
        var stack = new Stack<Node>();
        stack.Push(root);

        foreach (var heading in headings)
        {
            while (stack.Count > 1 && stack.Peek().Entry.Level >= heading.Level)
            {
                stack.Pop();
            }

            var node = new Node(heading);
            stack.Peek().Children.Add(node);
            stack.Push(node);
        }

        return root.Children.Select(child => Convert(child, parentPath: string.Empty)).ToArray();
    }

    private static DocumentSection Convert(Node node, string parentPath)
    {
        string title = node.Entry.Title ?? string.Empty;
        string path = parentPath + "/" + title;

        var location = node.Entry.Location is { } existing
            ? existing with { SectionPath = path }
            : new DocumentLocation(SectionPath: path);

        return new DocumentSection
        {
            Title = node.Entry.Title,
            Level = node.Entry.Level,
            Text = node.Entry.Text,
            Location = location,
            Children = node.Children.Select(child => Convert(child, path)).ToArray(),
        };
    }
}
