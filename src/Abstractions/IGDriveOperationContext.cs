using Google.Apis.Drive.v3;

namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// The minimal surface an operation group (e.g. <see cref="IGDriveFileOperations"/>) needs from
/// its owning client. This is the seam that decouples each group from the concrete
/// <see cref="GoogleDriveApi"/> facade: groups depend on this 2-member contract, not the god class.
/// <para>
/// <see cref="GoogleDriveApi"/> implements this interface, and the Folders/Transfers/Trash splits
/// will reuse the same seam — so its shape is an architectural decision, not plumbing.
/// </para>
/// </summary>
internal interface IGDriveOperationContext
{
    /// <summary>
    /// The authorized <see cref="DriveService"/>, accessed through the client's single guard point
    /// (throws if the client is disposed or not yet authorized).
    /// </summary>
    DriveService Provider { get; }

    /// <summary>
    /// The default parent folder id used when a caller does not specify one.
    /// </summary>
    string RootFolderId { get; }
}
