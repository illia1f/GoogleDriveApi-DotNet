namespace GoogleDriveApi_DotNet.Exceptions
{
    public class CreateMediaUploadException : GoogleDriveApiException
    {
        public CreateMediaUploadException() { }
        public CreateMediaUploadException(string message) : base(message) { }
        public CreateMediaUploadException(string message, Exception inner) : base(message, inner) { }
    }
}
