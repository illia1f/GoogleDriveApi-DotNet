namespace GoogleDriveApi_DotNet.Exceptions
{
    public class AuthorizationException : GoogleDriveApiException
    {
        public AuthorizationException() { }
        public AuthorizationException(string message) : base(message) { }
        public AuthorizationException(string message, Exception inner) : base(message, inner) { }
    }
}
