namespace DeepSigma.DocumentReader;

/// <summary>Options for the PowerPoint (PPTX) reader.</summary>
public sealed class PowerPointReadOptions : IFormatReadOptions
{
    /// <summary>Whether to extract slide body text. Default <see langword="true"/>.</summary>
    public bool ExtractSlideText { get; init; } = true;

    /// <summary>Whether to extract speaker notes. Default <see langword="true"/>.</summary>
    public bool ExtractSpeakerNotes { get; init; } = true;

    /// <summary>Whether to extract tables. Default <see langword="true"/>.</summary>
    public bool ExtractTables { get; init; } = true;

    /// <summary>Whether to read hidden slides. Default <see langword="false"/>.</summary>
    public bool IncludeHiddenSlides { get; init; }
}

/// <summary>A single slide within a presentation.</summary>
public sealed class PresentationSlide
{
    /// <summary>The 1-based slide number.</summary>
    public required int SlideNumber { get; init; }

    /// <summary>The slide title, if a title placeholder is present.</summary>
    public string? Title { get; init; }

    /// <summary>The slide's body text.</summary>
    public string? Text { get; init; }

    /// <summary>The slide's speaker notes.</summary>
    public string? SpeakerNotes { get; init; }

    /// <summary>Tables on the slide.</summary>
    public IReadOnlyList<DocumentTable> Tables { get; init; } = [];
}

/// <summary>Format-specific presentation details attached to a read result.</summary>
public sealed class PresentationDocumentFeature : IDocumentFeature
{
    /// <inheritdoc />
    public string Name => "Presentation";

    /// <summary>The slides in presentation order.</summary>
    public IReadOnlyList<PresentationSlide> Slides { get; init; } = [];
}
