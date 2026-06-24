using System.Text;

namespace DeepSigma.DocumentReader.Export;

/// <summary>Exports a result's plain-text projection, synthesizing one from structure when absent.</summary>
public sealed class TextResultExporter : IDocumentResultExporter
{
    /// <inheritdoc />
    public string Format => "text";

    /// <inheritdoc />
    public string ContentType => "text/plain";

    /// <inheritdoc />
    public async Task ExportAsync(DocumentReadResult result, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(output);

        string text = result.Text ?? Synthesize(result);
        await using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static string Synthesize(DocumentReadResult result)
    {
        var builder = new StringBuilder();
        AppendSections(builder, result.Sections);

        foreach (DocumentTable table in result.Tables)
        {
            foreach (DocumentTableRow row in table.Rows)
            {
                builder.AppendLine(string.Join('\t', row.Cells.Select(c => c.Text ?? string.Empty)));
            }
        }

        return builder.ToString();
    }

    private static void AppendSections(StringBuilder builder, IReadOnlyList<DocumentSection> sections)
    {
        foreach (DocumentSection section in sections)
        {
            if (!string.IsNullOrEmpty(section.Title))
            {
                builder.AppendLine(section.Title);
            }

            if (!string.IsNullOrEmpty(section.Text))
            {
                builder.AppendLine(section.Text);
            }

            AppendSections(builder, section.Children);
        }
    }
}
