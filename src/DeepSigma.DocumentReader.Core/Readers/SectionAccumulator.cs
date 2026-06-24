using System.Text;

namespace DeepSigma.DocumentReader.Core.Readers;

/// <summary>
/// A mutable accumulator for a section's heading and body text, used while scanning a
/// document in order. Collect a sequence of these, then project to <see cref="HeadingEntry"/>
/// values and build a tree with <see cref="SectionTreeBuilder"/>.
/// </summary>
public sealed class SectionAccumulator(int level, string title)
{
    /// <summary>The heading level (1 = top level).</summary>
    public int Level { get; } = level;

    /// <summary>The heading text.</summary>
    public string Title { get; } = title;

    /// <summary>The accumulated body text.</summary>
    public StringBuilder Body { get; } = new();

    /// <summary>Appends a line of body text, followed by a newline.</summary>
    public void AppendLine(string text) => Body.Append(text).Append('\n');

    /// <summary>Returns the body trimmed of trailing newlines, or <see langword="null"/> if empty.</summary>
    public string? BodyText() => Body.Length == 0 ? null : Body.ToString().TrimEnd('\n');
}
