namespace GoogleDriveApi_DotNet.Exceptions;

public class UpdateFileContentException : GoogleDriveApiException
{
    public UpdateFileContentException() { }
    public UpdateFileContentException(string message) : base(message) { }
    public UpdateFileContentException(string message, Exception inner) : base(message, inner) { }
}
