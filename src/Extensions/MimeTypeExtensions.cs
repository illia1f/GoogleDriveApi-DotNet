using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Extensions;

/// <summary>
/// Enforces the file/folder type partition for an operation. The type knowledge lives on
/// <see cref="MimeType"/>; these guards only decide whether to throw.
/// </summary>
internal static class MimeTypeExtensions
{
    /// <summary>
    /// Requires that the item is not a folder.
    /// </summary>
    /// <param name="mimeType">The item's MIME type.</param>
    /// <exception cref="InvalidMimeTypeException">Thrown when the item is a folder.</exception>
    public static void RequireFile(this MimeType mimeType)
    {
        if (mimeType.IsFolder)
        {
            throw InvalidMimeTypeException.For(mimeType.Value);
        }
    }

    /// <summary>
    /// Requires that the item is a folder.
    /// </summary>
    /// <param name="mimeType">The item's MIME type.</param>
    /// <exception cref="InvalidMimeTypeException">Thrown when the item is not a folder.</exception>
    public static void RequireFolder(this MimeType mimeType)
    {
        if (!mimeType.IsFolder)
        {
            throw InvalidMimeTypeException.For(mimeType.Value, expectedMimeType: MimeType.Folder);
        }
    }
}
