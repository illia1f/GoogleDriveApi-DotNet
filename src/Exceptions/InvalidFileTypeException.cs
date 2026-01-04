namespace GoogleDriveApi_DotNet.Exceptions;

public class InvalidFileTypeException : GoogleDriveApiException
{
    public InvalidFileTypeException() { }
    public InvalidFileTypeException(string message) : base(message) { }
    public InvalidFileTypeException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Creates a new instance with details about the MIME type mismatch.
    /// </summary>
    /// <param name="fileId">The ID of the item with the invalid type.</param>
    /// <param name="actualMimeType">The actual MIME type of the item.</param>
    /// <param name="expectedMimeType">
    /// The expected MIME type, or <c>null</c> if expecting any non-folder type.
    /// </param>
    public InvalidFileTypeException(string fileId, string actualMimeType, string? expectedMimeType = null)
        : base(expectedMimeType is not null
        ? $"Invalid MIME type '{actualMimeType}' for item '{fileId}'. Expected: '{expectedMimeType}'."
        : $"Invalid MIME type '{actualMimeType}' for item '{fileId}'.")
    { }
}
