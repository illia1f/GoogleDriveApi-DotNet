namespace GoogleDriveApi_DotNet.Exceptions;

public class UploadFileException : GoogleDriveApiException
{
    public UploadFileException() { }
    public UploadFileException(string message) : base(message) { }
    public UploadFileException(string message, Exception inner) : base(message, inner) { }
}
