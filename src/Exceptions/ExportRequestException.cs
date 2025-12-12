namespace GoogleDriveApi_DotNet.Exceptions
{
    public class ExportRequestException : GoogleDriveApiException
    {
        public ExportRequestException() { }
        public ExportRequestException(string message) : base(message) { }
        public ExportRequestException(string message, Exception inner) : base(message, inner) { }
    }
}