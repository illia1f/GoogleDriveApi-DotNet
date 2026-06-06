namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The exception that is thrown when a Google Drive file's MIME type cannot be downloaded or exported.
/// The condition is data-dependent (the type is stored server-side), so callers typically skip or report the file.
/// </summary>
public class UnsupportedMimeTypeException : GoogleDriveApiException
{
    /// <summary>
    /// Gets the ID of the Google Drive item with the unsupported MIME type, if available.
    /// </summary>
    public string? GDriveItemId { get; }

    /// <summary>
    /// Gets the unsupported MIME type, if available.
    /// </summary>
    public string? MimeType { get; }

    /// <inheritdoc/>
    public UnsupportedMimeTypeException() { }

    /// <inheritdoc/>
    public UnsupportedMimeTypeException(string message)
        : base(message)
    { }

    /// <inheritdoc/>
    public UnsupportedMimeTypeException(string message, Exception? innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Creates a new instance with details about the unsupported MIME type.
    /// </summary>
    /// <param name="gDriveItemId">The ID of the Google Drive item with the unsupported MIME type.</param>
    /// <param name="mimeType">The MIME type that is not supported for download or export.</param>
    public UnsupportedMimeTypeException(string gDriveItemId, string mimeType)
        : base($"Unsupported MIME type '{mimeType}' for item '{gDriveItemId}'.")
    {
        GDriveItemId = gDriveItemId;
        MimeType = mimeType;
    }
}
