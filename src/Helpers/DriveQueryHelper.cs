namespace GoogleDriveApi_DotNet.Helpers;

/// <summary>
/// Helpers for building Google Drive search queries (<c>request.Q</c>).
/// <para>Query language reference: https://developers.google.com/workspace/drive/api/guides/search-files</para>
/// </summary>
internal static class DriveQueryHelper
{
    /// <summary>
    /// Escapes a value for safe interpolation into a Drive search query.
    /// Backslashes and single quotes are special in the Drive query language; a name like
    /// <c>O'Brien</c> would otherwise produce an opaque HTTP 400 from the API.
    /// </summary>
    /// <param name="value">The raw value to escape (e.g., a file or folder name).</param>
    /// <returns>The escaped value, safe to place inside single quotes in a query.</returns>
    public static string EscapeValue(string value) =>
        value.Replace("\\", "\\\\").Replace("'", "\\'");
}
