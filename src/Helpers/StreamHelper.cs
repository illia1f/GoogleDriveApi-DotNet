namespace GoogleDriveApi_DotNet.Helpers
{
    public static class StreamHelper
    {
        public static void ResetIfSeekable(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (stream.CanSeek) stream.Position = 0;
        }
    }
}
