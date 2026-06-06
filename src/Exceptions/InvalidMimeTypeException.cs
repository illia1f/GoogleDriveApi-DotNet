namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The exception that is thrown when a Google Drive item has a different MIME type than the operation expects
/// (for example, passing a folder ID to a file operation, or vice versa).
/// </summary>
public class InvalidMimeTypeException : GoogleDriveApiException
{
    /// <inheritdoc/>
    public InvalidMimeTypeException() { }

    /// <inheritdoc/>
    public InvalidMimeTypeException(string message)
        : base(message)
    { }

    /// <inheritdoc/>
    public InvalidMimeTypeException(string message, Exception? innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Creates a new instance with details about the MIME type mismatch.
    /// </summary>
    /// <param name="gDriveItemId">The ID of the Google Drive item with the invalid type.</param>
    /// <param name="actualMimeType">The actual MIME type of the item.</param>
    /// <param name="expectedMimeType">
    /// The expected MIME type, or <c>null</c> if expecting any non-folder type.
    /// </param>
    public InvalidMimeTypeException(string gDriveItemId, string actualMimeType, string? expectedMimeType = null)
        : base(expectedMimeType is not null
        ? $"Invalid MIME type '{actualMimeType}' for item '{gDriveItemId}'. Expected: '{expectedMimeType}'."
        : $"Invalid MIME type '{actualMimeType}' for item '{gDriveItemId}'.")
    { }
}
