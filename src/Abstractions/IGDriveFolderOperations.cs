using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// Folder operations for Google Drive: find, list, list-all, create, delete, rename, and move.
/// Reads like the underlying <c>DriveService.Files.*</c> surface, scoped to folders.
/// </summary>
/// <remarks>
/// Obtained from <see cref="GoogleDriveApi.Folders"/>. If the owning client is not yet authorized,
/// the first member call authorizes it on demand.
/// </remarks>
public interface IGDriveFolderOperations
{
    /// <summary>
    /// Retrieves the ID of a folder by its name within the specified parent folder.
    /// </summary>
    /// <param name="folderName">The name of the folder to search for.</param>
    /// <param name="parentFolderId">Parent folder to search within. If <c>null</c>, the configured root folder is used.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The folder ID if a matching folder is found; otherwise, <c>null</c>.</returns>
    /// <remarks>
    /// The search is limited to non-trashed items and returns at most one result. If multiple folders
    /// with the same name exist in the same parent folder, the returned folder is unspecified.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="folderName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string?> FindIdByNameAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the folders within the specified parent folder, across all pages.
    /// </summary>
    /// <param name="parentFolderId">The ID of the parent folder to search within.</param>
    /// <param name="pageSize">Maximum number of folders per page. Must be greater than zero.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The ID and name of each non-trashed folder in the parent folder.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parentFolderId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is less than or equal to zero.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<IReadOnlyList<(string id, string name)>> ListAsync(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all folders in Google Drive, across all pages.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Every folder in Google Drive.</returns>
    /// <remarks>
    /// Each request uses the maximum supported page size (1000) and continues until all pages are fetched.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<IReadOnlyList<DriveItem>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new folder in Google Drive.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <param name="parentFolderId">Parent folder in which the new folder is created. If <c>null</c>, the configured root folder is used.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The ID of the newly created folder.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="folderName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string> CreateAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder to delete.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="folderId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to an item that is not a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task DeleteAsync(string folderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder to rename.</param>
    /// <param name="newName">The new name to assign to the folder.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="folderId"/> or <paramref name="newName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to an item that is not a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task RenameAsync(string folderId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a folder from <paramref name="sourceFolderId"/> to <paramref name="destinationFolderId"/>.
    /// </summary>
    /// <param name="folderId">The ID of the folder to move.</param>
    /// <param name="sourceFolderId">The ID of the parent folder from which the folder will be moved.</param>
    /// <param name="destinationFolderId">The ID of the parent folder to which the folder will be moved.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when any of <paramref name="folderId"/>, <paramref name="sourceFolderId"/>, or <paramref name="destinationFolderId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to an item that is not a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task MoveAsync(string folderId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default);
}
