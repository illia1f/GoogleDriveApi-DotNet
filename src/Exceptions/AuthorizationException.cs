namespace GoogleDriveApi_DotNet.Exceptions;

/// <summary>
/// The exception that is thrown when an operation requires authorization that has not been performed,
/// or when <see cref="GoogleDriveApi.AuthorizeAsync"/> is called on an already authorized instance.
/// </summary>
public class AuthorizationException : GoogleDriveApiException
{
    /// <inheritdoc/>
    public AuthorizationException() { }

    /// <inheritdoc/>
    public AuthorizationException(string message)
        : base(message) 
    { }

    /// <inheritdoc/>
    public AuthorizationException(string message, Exception? innerException)
        : base(message, innerException) 
    { }
}
