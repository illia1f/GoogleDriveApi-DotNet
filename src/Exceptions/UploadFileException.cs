namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The exception that is thrown when a file cannot be uploaded to Google Drive.
/// The underlying cause is available via <see cref="Exception.InnerException"/>.
/// </summary>
public class UploadFileException : GoogleDriveApiException
{
    /// <inheritdoc/>
    public UploadFileException() { }

    /// <inheritdoc/>
    public UploadFileException(string message)
        : base(message)
    { }

    /// <inheritdoc/>
    public UploadFileException(string message, Exception? innerException)
        : base(message, innerException)
    { }
}
