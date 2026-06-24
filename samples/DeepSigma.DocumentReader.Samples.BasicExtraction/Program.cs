using DeepSigma.DocumentReader;

// Minimal end-to-end sample: read a document and print its text projection.
// Usage: dotnet run -- <path-to-document>

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: BasicExtraction <path-to-document>");
    return 1;
}

string path = args[0];

IDocumentReader reader = DocumentReaderFactory.CreateDefault();

using DocumentSource source = DocumentSource.FromFile(path);
DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

Console.WriteLine($"Kind:    {result.Kind}");
Console.WriteLine($"Quality: {result.Quality}");
Console.WriteLine($"Tables:  {result.Tables.Count}");
Console.WriteLine($"Sections:{result.Sections.Count}");
if (result.Warnings.Count > 0)
{
    Console.WriteLine($"Warnings:{result.Warnings.Count}");
    foreach (DocumentWarning warning in result.Warnings)
    {
        Console.WriteLine($"  [{warning.Code}] {warning.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("----- text -----");
Console.WriteLine(result.Text);

return 0;
