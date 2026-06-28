namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// Transfer operations for Google Drive: upload (from a file path or a stream), update file
/// content, and download (with Google Workspace export handled automatically).
/// </summary>
/// <remarks>
/// Obtained from <see cref="GoogleDriveApi.Transfers"/>. If the owning client is not yet authorized,
/// the first member call authorizes it on demand.
/// </remarks>
public interface IDriveTransfers
{
    /// <summary>
    /// Uploads a file from the specified file system path to Google Drive using a resumable upload.
    /// </summary>
    /// <param name="filePath">The full path to the file to be uploaded.</param>
    /// <param name="mimeType">The MIME type of the file content (for example <c>application/pdf</c> or <c>image/png</c>).</param>
    /// <param name="parentFolderId">Parent folder in which the file is created. If <c>null</c>, the configured root folder is used.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The identifier of the newly created Google Drive file.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> or <paramref name="mimeType"/> is <c>null</c> or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified by <paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="Exceptions.UploadFileException">Thrown when the upload fails for any reason other than cancellation, including when no valid file identifier is returned.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    Task<string> UploadAsync(string filePath, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to Google Drive from the provided stream using a resumable upload.
    /// </summary>
    /// <param name="content">A stream containing the file content to be uploaded. The caller owns and disposes the stream.</param>
    /// <param name="fileName">The name of the file to be created in Google Drive.</param>
    /// <param name="mimeType">The MIME type of the file content.</param>
    /// <param name="parentFolderId">Parent folder in which the file is created. If <c>null</c>, the configured root folder is used.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The identifier of the newly created Google Drive file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> or <paramref name="mimeType"/> is <c>null</c> or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="Exceptions.UploadFileException">Thrown when the upload fails for any reason other than cancellation, including when no file identifier is returned.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    Task<string> UploadAsync(Stream content, string fileName, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the binary content of an existing Google Drive file using a resumable upload, preserving its metadata.
    /// </summary>
    /// <param name="fileId">The identifier of the file whose content should be updated.</param>
    /// <param name="content">A stream containing the new file content. The caller owns and disposes the stream. If seekable, its position is reset before upload.</param>
    /// <param name="contentType">The MIME type of the content (for example <c>application/pdf</c> or <c>image/png</c>).</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> or <paramref name="contentType"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="Exceptions.UpdateFileContentException">Thrown when the upload fails or does not complete successfully.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    Task UpdateContentAsync(string fileId, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a Google Drive file to the specified local directory. Google Workspace files
    /// (Docs, Sheets, Slides, etc.) are exported to a compatible format; all other files are downloaded as-is.
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <param name="saveToPath">The local directory where the downloaded file is saved. Created if it does not exist. Defaults to <c>Downloads</c>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> or <paramref name="saveToPath"/> is <c>null</c> or empty.</exception>
    /// <exception cref="Exceptions.UnsupportedMimeTypeException">Thrown when the file's MIME type is not supported for download or export.</exception>
    /// <exception cref="Exceptions.DownloadFileException">Thrown when the file cannot be downloaded, exported, or saved locally.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning client has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task DownloadAsync(string fileId, string saveToPath = "Downloads", CancellationToken cancellationToken = default);
}
