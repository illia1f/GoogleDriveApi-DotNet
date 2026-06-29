using Google.Apis.Drive.v3;

namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// The minimal surface an operation group (e.g. <see cref="IDriveFiles"/>) needs from its
/// owning client: how to obtain the authorized service and the default root folder. Decouples each
/// group from the concrete <see cref="GoogleDriveApi"/> client so groups depend on this seam, not the client.
/// </summary>
internal interface IDriveOperationContext
{
    /// <summary>
    /// Returns the authorized <see cref="DriveService"/>, authorizing on first use.
    /// The call is lazy, idempotent and thread-safe: the first caller performs the one-time
    /// authorization; subsequent callers get the cached service without re-authorizing.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    ValueTask<DriveService> GetServiceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The default parent folder id used when a caller does not specify one.
    /// </summary>
    string RootFolderId { get; }
}
