namespace GoogleDriveApi_DotNet.Helpers;

public static class MimeTypeHelper
{
    /// <summary>
    /// Returns a file extension for a given MIME type.
    /// </summary>
    /// <param name="mimeType">A string representing the MIME type for which to determine the file extension.</param>
    /// <returns>The corresponding file extension, or null if no extension is found.</returns>
    public static string? GetExtensionBy(string mimeType)
    {
        return MimeMapping.MimeUtility.GetExtensions(mimeType).FirstOrDefault();
    }
}