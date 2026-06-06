using System.Text;

namespace GoogleDriveApi_DotNet.Helpers;

internal static class PathHelper
{
    /// <summary>
    /// Replaces characters that are invalid in file names (e.g. <c>:</c>, <c>/</c>, <c>*</c>)
    /// with underscores. Google Drive allows such characters in file names, but local file
    /// systems do not (on Windows, a colon even silently redirects the content into an NTFS
    /// alternate data stream, producing an empty file).
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <returns>The file name with all invalid characters replaced by underscores.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileName"/> is null.</exception>
    public static string SanitizeFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        char[] invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);

        foreach (char c in fileName)
        {
            builder.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
        }

        return builder.ToString();
    }
}
