namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The exception that is thrown when a Google Drive file cannot be downloaded, exported, or saved locally.
/// The underlying cause is available via <see cref="Exception.InnerException"/>.
/// </summary>
public class DownloadFileException : GoogleDriveApiException
{
    /// <inheritdoc/>
    public DownloadFileException() { }

    /// <inheritdoc/>
    public DownloadFileException(string message)
        : base(message) 
    { }

    /// <inheritdoc/>
    public DownloadFileException(string message, Exception? innerException)
        : base(message, innerException) 
    { }
}
