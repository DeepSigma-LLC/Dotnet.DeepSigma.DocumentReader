using System.Text;
using DeepSigma.DocumentReader.Export.Internal;

namespace DeepSigma.DocumentReader.Export;

/// <summary>
/// Exports a result as Markdown: a small metadata front-matter block, the text projection,
/// and any tables rendered as pipe tables. This is the richest, most RAG-friendly export.
/// </summary>
public sealed class MarkdownResultExporter : IDocumentResultExporter
{
    /// <inheritdoc />
    public string Format => "markdown";

    /// <inheritdoc />
    public string ContentType => "text/markdown";

    /// <inheritdoc />
    public async Task ExportAsync(DocumentReadResult result, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(output);

        var builder = new StringBuilder();
        WriteFrontMatter(builder, result);

        // The Markdown projection already contains its own tables, so emit it verbatim.
        // For tabular kinds the extracted tables ARE the content, so render those instead of
        // the raw delimited text. Otherwise emit the text projection.
        bool renderTablesInsteadOfText = result.Kind != DocumentKind.Markdown && result.Tables.Count > 0;

        if (!renderTablesInsteadOfText && !string.IsNullOrEmpty(result.Text))
        {
            builder.AppendLine(result.Text.TrimEnd('\n', '\r'));
            builder.AppendLine();
        }

        if (renderTablesInsteadOfText)
        {
            foreach (DocumentTable table in result.Tables)
            {
                if (!string.IsNullOrEmpty(table.Name))
                {
                    builder.Append("## ").AppendLine(table.Name);
                }

                PipeTableWriter.Write(builder, table);
                builder.AppendLine();
            }
        }

        await using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteAsync(builder.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static void WriteFrontMatter(StringBuilder builder, DocumentReadResult result)
    {
        builder.AppendLine("---");
        builder.Append("kind: ").AppendLine(result.Kind.ToString());
        builder.Append("quality: ").AppendLine(result.Quality.ToString());
        if (!string.IsNullOrEmpty(result.Source.FileName))
        {
            builder.Append("source: ").AppendLine(result.Source.FileName);
        }

        if (!string.IsNullOrEmpty(result.Metadata.Title))
        {
            builder.Append("title: ").AppendLine(result.Metadata.Title);
        }

        builder.AppendLine("---");
        builder.AppendLine();
    }
}
