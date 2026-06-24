namespace DeepSigma.DocumentReader.Core.Detection;

/// <summary>
/// A single source of evidence for document type detection. Signals inspect the
/// <see cref="DetectionContext"/> and contribute scored candidates. The composite detector
/// runs all registered signals and combines their candidates.
/// </summary>
public interface IDetectionSignal
{
    /// <summary>A short name identifying this signal (used to attribute candidates).</summary>
    string Name { get; }

    /// <summary>Inspects the context and contributes zero or more candidates.</summary>
    void Evaluate(DetectionContext context);
}
