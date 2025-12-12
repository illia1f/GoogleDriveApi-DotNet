namespace GoogleDriveApi_DotNet.Exceptions
{
    public class GoogleDriveApiException : Exception
    {
        public GoogleDriveApiException() { }
        public GoogleDriveApiException(string message) : base(message) { }
        public GoogleDriveApiException(string message, Exception inner) : base(message, inner) { }
    }
}
