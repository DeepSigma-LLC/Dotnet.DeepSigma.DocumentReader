namespace DeepSigma.DocumentReader;

/// <summary>Base type for all exceptions thrown by the document reader.</summary>
public class DocumentReaderException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public DocumentReaderException(string message) : base(message) { }

    /// <summary>Initializes a new instance with an inner exception.</summary>
    public DocumentReaderException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>Thrown when no registered reader can handle a source.</summary>
public sealed class UnsupportedDocumentTypeException : DocumentReaderException
{
    /// <summary>Initializes a new instance for the given source description.</summary>
    public UnsupportedDocumentTypeException(string? fileName, DocumentKind detectedKind)
        : base($"No reader is registered for kind '{detectedKind}'" +
               (fileName is null ? "." : $" (source '{fileName}')."))
    {
        FileName = fileName;
        DetectedKind = detectedKind;
    }

    /// <summary>The source file name, if known.</summary>
    public string? FileName { get; }

    /// <summary>The detected kind that could not be handled.</summary>
    public DocumentKind DetectedKind { get; }
}

/// <summary>Thrown when a source exceeds the configured maximum size.</summary>
public sealed class DocumentSizeLimitExceededException : DocumentReaderException
{
    /// <summary>Initializes a new instance.</summary>
    public DocumentSizeLimitExceededException(long limitBytes)
        : base($"The document exceeds the configured maximum size of {limitBytes} bytes.")
    {
        LimitBytes = limitBytes;
    }

    /// <summary>The configured limit in bytes.</summary>
    public long LimitBytes { get; }
}

/// <summary>Thrown when a read operation exceeds its configured timeout with no usable result.</summary>
public sealed class DocumentTimeoutException : DocumentReaderException
{
    /// <summary>Initializes a new instance.</summary>
    public DocumentTimeoutException(TimeSpan timeout)
        : base($"Reading the document exceeded the configured timeout of {timeout}.")
    {
        Timeout = timeout;
    }

    /// <summary>The configured timeout.</summary>
    public TimeSpan Timeout { get; }
}

/// <summary>Thrown when a source's stream is missing, unreadable, or otherwise invalid.</summary>
public sealed class InvalidDocumentSourceException : DocumentReaderException
{
    /// <summary>Initializes a new instance.</summary>
    public InvalidDocumentSourceException(string message) : base(message) { }
}

/// <summary>Thrown when a document is password-protected or encrypted and cannot be read.</summary>
public sealed class DocumentProtectedException : DocumentReaderException
{
    /// <summary>Initializes a new instance.</summary>
    public DocumentProtectedException(string message) : base(message) { }
}
