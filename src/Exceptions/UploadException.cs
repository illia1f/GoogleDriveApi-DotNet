namespace GoogleDriveApi_DotNet.Exceptions
{
    public class UploadException : GoogleDriveApiException
    {
        public UploadException() { }
        public UploadException(string message) : base(message) { }
        public UploadException(string message, Exception inner) : base(message, inner) { }
    }
}
