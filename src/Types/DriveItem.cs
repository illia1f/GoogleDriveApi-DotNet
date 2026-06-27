namespace GoogleDriveApi_DotNet.Types;

/// <summary>
/// A read model for a Google Drive item (file or folder). Query-only: it answers questions about the
/// item (such as <see cref="IsFolder"/>) but performs no actions and throws no operation exceptions.
/// </summary>
public sealed record DriveItem
{
    /// <summary>
    /// The item's ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The item's name. Always populated: the mapper requires the source <c>name</c> field, so any
    /// call that builds a <see cref="DriveItem"/> must include <c>name</c> in its Drive <c>Fields</c> mask.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The item's MIME type.
    /// </summary>
    public required MimeType MimeType { get; init; }

    /// <summary>
    /// Gets a value indicating whether this item is a folder.
    /// </summary>
    public bool IsFolder => MimeType.IsFolder;
}
