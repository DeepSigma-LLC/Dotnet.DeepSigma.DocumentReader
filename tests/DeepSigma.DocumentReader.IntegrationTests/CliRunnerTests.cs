using DeepSigma.DocumentReader;
using DeepSigma.DocumentReader.Cli;
using DeepSigma.DocumentReader.Export;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DeepSigma.DocumentReader.IntegrationTests;

public sealed class CliRunnerTests
{
    private static (CliRunner Runner, StringWriter Out, StringWriter Err) CreateRunner()
    {
        var services = new ServiceCollection();
        services.AddDeepSigmaDocumentReaderDefaults();
        ServiceProvider provider = services.BuildServiceProvider();

        var output = new StringWriter();
        var error = new StringWriter();
        var runner = new CliRunner(
            provider.GetRequiredService<IDocumentReader>(),
            provider.GetRequiredService<IDocumentTypeDetector>(),
            provider.GetRequiredService<IExporterResolver>(),
            output,
            error);
        return (runner, output, error);
    }

    [Fact]
    public async Task Detect_reports_json_kind()
    {
        var (runner, output, _) = CreateRunner();

        int exit = await runner.DetectAsync(TestPaths.Corpus("Json", "sample.json"), json: false, CancellationToken.None);

        Assert.Equal(CliRunner.ExitSuccess, exit);
        Assert.Contains("Json", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Extract_csv_to_markdown_writes_pipe_table()
    {
        var (runner, _, _) = CreateRunner();
        string outputPath = Path.Combine(Path.GetTempPath(), $"dsread-test-{Guid.NewGuid():N}.md");
        try
        {
            int exit = await runner.ExtractAsync(
                TestPaths.Corpus("Csv", "sample.csv"),
                "markdown",
                outputPath,
                DocumentReadOptions.Default,
                strict: false,
                CancellationToken.None);

            Assert.Equal(CliRunner.ExitSuccess, exit);
            string content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("| name | age | city |", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task Extract_unsupported_format_returns_unsupported_exit_code()
    {
        var (runner, _, _) = CreateRunner();

        int exit = await runner.ExtractAsync(
            TestPaths.Corpus("Text", "sample.txt"),
            "pdf",
            outputPath: Path.Combine(Path.GetTempPath(), "unused.pdf"),
            DocumentReadOptions.Default,
            strict: false,
            CancellationToken.None);

        Assert.Equal(CliRunner.ExitUnsupported, exit);
    }
}
