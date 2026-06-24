namespace DeepSigma.DocumentReader;

/// <summary>
/// A non-fatal problem encountered while reading a document. Readers prefer returning
/// warnings alongside a partial result over throwing.
/// </summary>
public sealed class DocumentWarning
{
    /// <summary>A stable, machine-readable code (see <see cref="WarningCodes"/>).</summary>
    public required string Code { get; init; }

    /// <summary>A human-readable description of the problem.</summary>
    public required string Message { get; init; }

    /// <summary>Where in the document the problem occurred, if known.</summary>
    public DocumentLocation? Location { get; init; }

    /// <summary>The underlying exception, when the warning originated from one.</summary>
    public Exception? Exception { get; init; }
}
