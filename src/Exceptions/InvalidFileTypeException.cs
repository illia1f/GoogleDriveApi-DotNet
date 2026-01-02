namespace GoogleDriveApi_DotNet.Exceptions
{
    public class InvalidFileTypeException : GoogleDriveApiException
    {
        public InvalidFileTypeException() { }
        public InvalidFileTypeException(string message) : base(message) { }
        public InvalidFileTypeException(string message, Exception inner) : base(message, inner) { }
        public InvalidFileTypeException(string fileId, string actualMimeType)
        : base($"Invalid MIME type '{actualMimeType}' for item '{fileId}'. A file was expected.")
        {
        }
    }
}
