namespace GoogleDriveApi_DotNet.Helpers;

public static class MimeTypeHelper
{
    /// <summary>
    /// Checks if a given <paramref name="mimeType"/> is specific to Google Workspace and Drive.
    /// <para>Documentation: https://developers.google.com/drive/api/guides/mime-types?hl=en</para>
    /// </summary>
    /// <param name="mimeType">A string representig the MIME type to be checked.</param>
    /// <returns>A boolean value indicating whether the MIME type is specific to Google Workspace and Google Drive.</returns>
    public static bool IsGDriveMimeType(string mimeType)
    {
        return mimeType.StartsWith("application/vnd.google-apps");
    }

    /// <summary>
    /// Maps Google-specific MIME types to their equivalent exportable MIME types.
    /// Google-specific MIME types are used by Google Drive to represent Google Workspace files, 
    /// such as Google Docs, Sheets, Slides, and Drawings. These MIME types are not directly 
    /// exportable as standard file formats (e.g., .docx, .xlsx, .pptx). Instead, they need to be 
    /// exported to a compatible MIME type for downloading.
    /// <para>Documentation: https://developers.google.com/drive/api/guides/ref-export-formats?hl=en</para>
    /// </summary>
    /// <param name="mimeType">A string representing the specific MIME type to be mapped to an exportable one.</param>
    /// <returns>A string representing the exportable MIME type, or null if the MIME type is not recognized.</returns>
    public static string? GetExportMimeTypeBy(string mimeType)
    {
        return mimeType switch
        {
            "application/vnd.google-apps.document" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.google-apps.spreadsheet" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.google-apps.presentation" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.google-apps.drawing" => "image/png",
            _ => null,
        };
    }

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