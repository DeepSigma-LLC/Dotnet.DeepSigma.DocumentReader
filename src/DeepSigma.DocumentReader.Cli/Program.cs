using System.CommandLine;
using DeepSigma.DocumentReader;
using DeepSigma.DocumentReader.Cli;
using DeepSigma.DocumentReader.Export;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddDeepSigmaDocumentReaderDefaults();
using var provider = services.BuildServiceProvider();

var runner = new CliRunner(
    provider.GetRequiredService<IDocumentReader>(),
    provider.GetRequiredService<IDocumentTypeDetector>(),
    provider.GetRequiredService<IExporterResolver>(),
    Console.Out,
    Console.Error);

var root = new RootCommand("dsread — DeepSigma document reader CLI.");
root.Subcommands.Add(BuildDetectCommand(runner));
root.Subcommands.Add(BuildExtractCommand(runner));
root.Subcommands.Add(BuildInspectCommand(runner));
root.Subcommands.Add(BuildBatchCommand(runner));

return await root.Parse(args).InvokeAsync().ConfigureAwait(false);

static Command BuildDetectCommand(CliRunner runner)
{
    var input = new Argument<string>("input") { Description = "Path to the document." };
    var json = new Option<bool>("--json") { Description = "Emit the result as JSON." };

    var command = new Command("detect", "Detect a document's type.");
    command.Arguments.Add(input);
    command.Options.Add(json);
    command.SetAction((parse, ct) => runner.DetectAsync(parse.GetValue(input)!, parse.GetValue(json), ct));
    return command;
}

static Command BuildExtractCommand(CliRunner runner)
{
    var input = new Argument<string>("input") { Description = "Path to the document." };
    var format = new Option<string>("--format", "-f") { Description = "Output format: text, markdown, or json.", DefaultValueFactory = _ => "text" };
    var output = new Option<string?>("--output", "-o") { Description = "Write to this file instead of stdout." };
    var strict = new Option<bool>("--strict") { Description = "Return a non-zero exit code if any warnings are raised." };
    var maxBytes = new Option<long?>("--max-bytes") { Description = "Maximum source size in bytes." };
    var maxRows = new Option<int?>("--max-rows") { Description = "Maximum CSV rows." };
    var maxDepth = new Option<int?>("--max-depth") { Description = "Maximum JSON nesting depth." };
    var timeout = new Option<int?>("--timeout") { Description = "Overall timeout in seconds." };

    var command = new Command("extract", "Read a document and export its contents.");
    command.Arguments.Add(input);
    foreach (var option in new Option[] { format, output, strict, maxBytes, maxRows, maxDepth, timeout })
    {
        command.Options.Add(option);
    }

    command.SetAction((parse, ct) => runner.ExtractAsync(
        parse.GetValue(input)!,
        parse.GetValue(format)!,
        parse.GetValue(output),
        BuildOptions(parse.GetValue(maxBytes), parse.GetValue(maxRows), parse.GetValue(maxDepth), parse.GetValue(timeout)),
        parse.GetValue(strict),
        ct));
    return command;
}

static Command BuildInspectCommand(CliRunner runner)
{
    var input = new Argument<string>("input") { Description = "Path to the document." };
    var json = new Option<bool>("--json") { Description = "Emit the full result as JSON." };
    var maxBytes = new Option<long?>("--max-bytes") { Description = "Maximum source size in bytes." };

    var command = new Command("inspect", "Summarize what a document contains.");
    command.Arguments.Add(input);
    command.Options.Add(json);
    command.Options.Add(maxBytes);
    command.SetAction((parse, ct) => runner.InspectAsync(
        parse.GetValue(input)!,
        BuildOptions(parse.GetValue(maxBytes), null, null, null),
        parse.GetValue(json),
        ct));
    return command;
}

static Command BuildBatchCommand(CliRunner runner)
{
    var input = new Argument<string>("input") { Description = "Input directory." };
    var output = new Option<string>("--output", "-o") { Description = "Output directory.", Required = true };
    var format = new Option<string>("--format", "-f") { Description = "Output format: text, markdown, or json.", DefaultValueFactory = _ => "markdown" };
    var recursive = new Option<bool>("--recursive", "-r") { Description = "Recurse into subdirectories." };

    var command = new Command("batch", "Read every file in a directory and export each.");
    command.Arguments.Add(input);
    command.Options.Add(output);
    command.Options.Add(format);
    command.Options.Add(recursive);
    command.SetAction((parse, ct) => runner.BatchAsync(
        parse.GetValue(input)!,
        parse.GetValue(output)!,
        parse.GetValue(format)!,
        parse.GetValue(recursive),
        DocumentReadOptions.Default,
        ct));
    return command;
}

static DocumentReadOptions BuildOptions(long? maxBytes, int? maxRows, int? maxDepth, int? timeoutSeconds)
{
    var options = new DocumentReadOptions
    {
        MaxBytes = maxBytes ?? DocumentReadOptions.DefaultMaxBytes,
        Timeout = timeoutSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
    };

    if (maxRows is not null)
    {
        options = options.WithOptions(new CsvReadOptions { MaxRows = maxRows });
    }

    if (maxDepth is not null)
    {
        options = options.WithOptions(new JsonReadOptions { MaxDepth = maxDepth });
    }

    return options;
}
