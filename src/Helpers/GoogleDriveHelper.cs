using System.Text.RegularExpressions;

namespace GoogleDriveApi_DotNet.Helpers;

public static class GoogleDriveHelper
{
    private const string RootFolderUrl = "https://drive.google.com/drive/my-drive";
    private const string FolderUrlPattern = @"https://drive.google.com/drive/folders/([a-zA-Z0-9-_]+)";
    private const string FolderBaseUrl = "https://drive.google.com/drive/folders/";

    /// <summary>
    /// Extracts the folder ID from a given URL.
    /// </summary>
    /// <param name="url">The URL to extract the folder ID from.</param>
    /// <returns>The extracted folder ID if found; otherwise, null.</returns>
    public static string ExtractFolderIdFromUrl(string url)
    {
        var match = Regex.Match(url, FolderUrlPattern);
        return match.Success ? match.Groups[1].Value : "root";
    }

    /// <summary>
    /// Generates a URL based on the provided folder ID.
    /// </summary>
    /// <param name="folderId">The folder ID to generate the URL for.</param>
    /// <returns>The generated URL.</returns>
    public static string GenerateUrlFromFolderId(string folderId)
    {
        if (folderId == "root")
        {
            return RootFolderUrl;
        }

        return $"{FolderBaseUrl}{folderId}";
    }
}