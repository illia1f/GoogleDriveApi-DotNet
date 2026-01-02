namespace GoogleDriveApi_DotNet.Exceptions
{
    public class InvalidFileTypeException : GoogleDriveApiException
    {
        public InvalidFileTypeException() { }
        public InvalidFileTypeException(string message) : base(message) { }
        public InvalidFileTypeException(string message, Exception inner) : base(message, inner) { }
    }
}
