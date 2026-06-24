namespace DeepSigma.DocumentReader.IntegrationTests;

/// <summary>Locates repository-relative paths (corpus, golden files) from the test binary.</summary>
internal static class TestPaths
{
    private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    public static string CorpusDirectory => Path.Combine(RepoRoot, "tests", "Corpus");

    public static string Corpus(params string[] segments)
        => Path.Combine([CorpusDirectory, .. segments]);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DeepSigma.DocumentReader.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root (DeepSigma.DocumentReader.slnx).");
    }
}
