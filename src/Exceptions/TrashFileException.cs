namespace GoogleDriveApi_DotNet.Exceptions;

public class TrashFileException : GoogleDriveApiException
{
    public TrashFileException() { }
    public TrashFileException(string message) : base(message) { }
    public TrashFileException(string message, Exception inner) : base(message, inner) { }
}
