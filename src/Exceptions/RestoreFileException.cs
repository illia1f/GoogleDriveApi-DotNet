namespace GoogleDriveApi_DotNet.Exceptions;

public class RestoreFileException : GoogleDriveApiException
{
    public RestoreFileException() { }
    public RestoreFileException(string message) : base(message) { }
    public RestoreFileException(string message, Exception inner) : base(message, inner) { }
}
