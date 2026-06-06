namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The exception that is thrown when the content of an existing Google Drive file cannot be updated.
/// The underlying cause is available via <see cref="Exception.InnerException"/>.
/// </summary>
public class UpdateFileContentException : GoogleDriveApiException
{
    /// <inheritdoc/>
    public UpdateFileContentException() { }

    /// <inheritdoc/>
    public UpdateFileContentException(string message)
        : base(message)
    { }

    /// <inheritdoc/>
    public UpdateFileContentException(string message, Exception? innerException)
        : base(message, innerException)
    { }
}
