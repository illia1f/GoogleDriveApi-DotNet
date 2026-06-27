using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// Trash operations for Google Drive: move to trash, restore, empty, and list trashed items.
/// </summary>
/// <remarks>
/// Obtained from <see cref="GoogleDriveApi.Trash"/>. If the owning client is not yet authorized,
/// the first member call authorizes it on demand.
/// </remarks>
public interface IGDriveTrashOperations
{
    /// <summary>
    /// Moves a file to the Google Drive trash by marking it as trashed.
    /// </summary>
    /// <param name="fileId">The ID of the file to move to trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task TrashAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a file from the Google Drive trash by marking it as not trashed.
    /// </summary>
    /// <param name="fileId">The ID of the file to restore.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task RestoreAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes all items from the Google Drive trash.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task EmptyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all items currently located in the Google Drive trash, across all pages, as the
    /// library's <see cref="DriveItem"/> model (<c>id</c>, <c>name</c>, <c>mimeType</c>).
    /// </summary>
    /// <param name="pageSize">Maximum number of items per page. Must be greater than zero.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The trashed items.</returns>
    /// <remarks>
    /// To fetch additional metadata without over-fetching, use the
    /// <see cref="ListAsync(DriveFields, int, CancellationToken)"/> overload.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is less than or equal to zero.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<IReadOnlyList<DriveItem>> ListAsync(int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all items currently located in the Google Drive trash, across all pages, as raw
    /// <see cref="GoogleFile"/> items carrying exactly the fields named by <paramref name="fields"/>.
    /// </summary>
    /// <param name="fields">The fields to fetch. Start from <see cref="DriveFields.Default"/> and chain <c>With*</c> calls.</param>
    /// <param name="pageSize">Maximum number of items per page. Must be greater than zero.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The trashed items, populated with the requested fields.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fields"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is less than or equal to zero.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<IReadOnlyList<GoogleFile>> ListAsync(DriveFields fields, int pageSize = 50, CancellationToken cancellationToken = default);
}
