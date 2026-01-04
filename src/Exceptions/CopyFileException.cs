namespace GoogleDriveApi_DotNet.Exceptions;

public class CopyFileException : GoogleDriveApiException
{
    public CopyFileException() { }
    public CopyFileException(string message) : base(message) { }
    public CopyFileException(string message, Exception inner) : base(message, inner) { }
}
