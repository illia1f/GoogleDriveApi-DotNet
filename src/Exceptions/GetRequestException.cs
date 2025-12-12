namespace GoogleDriveApi_DotNet.Exceptions
{
    public class GetRequestException : GoogleDriveApiException
    {
        public GetRequestException() { }
        public GetRequestException(string message) : base(message) { }
        public GetRequestException(string message, Exception inner) : base(message, inner) { }
    }
}