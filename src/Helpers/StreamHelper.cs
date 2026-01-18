namespace GoogleDriveApi_DotNet.Helpers
{
    public static class StreamHelper
    {
        /// <summary>
        /// Resets the stream position to the beginning if the stream supports seeking.
        /// </summary>
        /// <param name="stream">The stream whose position should be reset.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
        public static void ResetIfSeekable(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (stream.CanSeek) stream.Position = 0;
        }
    }
}
