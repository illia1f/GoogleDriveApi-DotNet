namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The base exception for all errors raised by this library.
/// Catch this type to handle any library-specific failure.
/// </summary>
public class GoogleDriveApiException : Exception
{
    /// <inheritdoc/>
    public GoogleDriveApiException() { }

    /// <inheritdoc/>
    public GoogleDriveApiException(string message)
        : base(message) 
    { }

    /// <inheritdoc/>
    public GoogleDriveApiException(string message, Exception? innerException)
        : base(message, innerException)
    { }
}
