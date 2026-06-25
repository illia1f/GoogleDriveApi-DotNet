namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// File operations for Google Drive: list, find, delete, rename, move, and copy.
/// Reads like the underlying <c>DriveService.Files.*</c> surface.
/// </summary>
/// <remarks>
/// Obtained from <see cref="GoogleDriveApi.Files"/>. If the owning client is not yet authorized,
/// the first member call authorizes it on demand.
/// </remarks>
public interface IGDriveFileOperations
{
    /// <summary>
    /// Retrieves the files (non-folders) within the specified parent folder, across all pages.
    /// </summary>
    /// <param name="parentFolderId">Parent folder to search within. If <c>null</c>, the configured root folder is used.</param>
    /// <param name="pageSize">Maximum number of files per page. Must be greater than zero.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The non-trashed, non-folder items in the folder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is less than or equal to zero.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<IReadOnlyList<GoogleFile>> ListAsync(string? parentFolderId = null, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the ID of a file by its name within the specified parent folder.
    /// </summary>
    /// <param name="fullFileName">The file name (including extension) to search for.</param>
    /// <param name="parentFolderId">Parent folder to search within. If <c>null</c>, the configured root folder is used.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The file ID if a matching file is found; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fullFileName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string?> FindIdByNameAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a file.
    /// </summary>
    /// <param name="fileId">The ID of the file to delete.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a file.
    /// </summary>
    /// <param name="fileId">The ID of the file to rename.</param>
    /// <param name="newName">The new name to assign to the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> or <paramref name="newName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task RenameAsync(string fileId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file from <paramref name="sourceFolderId"/> to <paramref name="destinationFolderId"/>.
    /// </summary>
    /// <param name="fileId">The ID of the file to move.</param>
    /// <param name="sourceFolderId">The ID of the folder from which the file will be moved.</param>
    /// <param name="destinationFolderId">The ID of the folder to which the file will be moved.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when any of <paramref name="fileId"/>, <paramref name="sourceFolderId"/>, or <paramref name="destinationFolderId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task MoveAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file into the specified destination folder.
    /// </summary>
    /// <param name="fileId">The ID of the file to copy.</param>
    /// <param name="destinationFolderId">The ID of the folder where the copied file will be placed.</param>
    /// <param name="newName">Optional new name for the copy. If <c>null</c> or empty, the original name is preserved.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The ID of the newly created copied file.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> or <paramref name="destinationFolderId"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.InvalidMimeTypeException">Thrown when the specified ID refers to a folder.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string> CopyAsync(string fileId, string destinationFolderId, string? newName = null, CancellationToken cancellationToken = default);
}
