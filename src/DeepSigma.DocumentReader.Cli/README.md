# DeepSigma.DocumentReader.Cli (`dsread`)

Command-line tool for the DeepSigma.DocumentReader ecosystem: detect, extract, inspect, and
batch-process documents.

```bash
dotnet tool install --global DeepSigma.DocumentReader.Cli
```

## Commands

```bash
dsread detect  input.json
dsread extract input.md  --format json --output out.json
dsread inspect input.csv
dsread batch   ./inputs --output ./out --format markdown --recursive
```

`extract` accepts `--max-bytes`, `--max-rows`, `--max-depth`, and `--timeout`; `--strict`
returns a non-zero exit code when warnings are raised. Use the CLI for smoke testing,
debugging parser behavior, generating expected test outputs, and batch extraction.

See the [full documentation](https://github.com/DeepSigma/Dotnet.DeepSigma.DocumentReader#readme).
