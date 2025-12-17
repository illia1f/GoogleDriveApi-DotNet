namespace GoogleDriveApi_DotNet.Types;

/// <summary>
/// Contains constants for Google Drive MIME types.
/// <para>Documentation: https://developers.google.com/drive/api/guides/mime-types?hl=en</para>
/// </summary>
public static class GDriveMimeTypes
{
    /// <summary>
    /// The prefix for all Google Workspace MIME types.
    /// </summary>
    public const string GoogleAppsPrefix = "application/vnd.google-apps";

    /// <summary>
    /// MIME type for Google Drive folders.
    /// </summary>
    public const string Folder = "application/vnd.google-apps.folder";

    /// <summary>
    /// MIME type for Google Docs.
    /// </summary>
    public const string Document = "application/vnd.google-apps.document";

    /// <summary>
    /// MIME type for Google Sheets.
    /// </summary>
    public const string Spreadsheet = "application/vnd.google-apps.spreadsheet";

    /// <summary>
    /// MIME type for Google Slides.
    /// </summary>
    public const string Presentation = "application/vnd.google-apps.presentation";

    /// <summary>
    /// MIME type for Google Drawings.
    /// </summary>
    public const string Drawing = "application/vnd.google-apps.drawing";

    /// <summary>
    /// Checks if a given <paramref name="mimeType"/> is specific to Google Workspace and Drive.
    /// <para>Documentation: https://developers.google.com/drive/api/guides/mime-types?hl=en</para>
    /// </summary>
    /// <param name="mimeType">A string representig the MIME type to be checked.</param>
    /// <returns>A boolean value indicating whether the MIME type is specific to Google Workspace and Google Drive.</returns>
    public static bool IsValid(string mimeType)
    {
        return !string.IsNullOrWhiteSpace(mimeType) && mimeType.StartsWith(GoogleAppsPrefix, StringComparison.OrdinalIgnoreCase);
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
            Document => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Spreadsheet => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Presentation => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            Drawing => "image/png",
            _ => null,
        };
    }
}