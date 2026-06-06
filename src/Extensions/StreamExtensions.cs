namespace GoogleDriveApi_DotNet.Extensions;

internal static class StreamExtensions
{
    /// <summary>
    /// Resets the stream position to the beginning if the stream supports seeking.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public static void ResetIfSeekable(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    /// <summary>
    /// Writes the entire content of the memory stream to a file, creating or overwriting it.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty.</exception>
    public static void SaveToFile(this MemoryStream stream, string path)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(path);

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);

        stream.WriteTo(fileStream);
    }
}
