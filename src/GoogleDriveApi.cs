using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Operations;
using GoogleDriveApi_DotNet.Types;
using System.Diagnostics;
using static Google.Apis.Drive.v3.FilesResource;

namespace GoogleDriveApi_DotNet;

/// <summary>
/// Provides a simplified wrapper around the Google Drive v3 API.
/// </summary>
/// <remarks>
/// Most instance members require successful authorization through <see cref="AuthorizeAsync"/>
/// before they are used. Each member documents the exceptions it can throw.
/// </remarks>
public class GoogleDriveApi : IDisposable, IGDriveOperationContext
{
    private readonly GoogleDriveApiOptions _options;
    private readonly IGoogleDriveAuthProvider _authProvider;
    private readonly SemaphoreSlim _authGate = new(1, 1);
    private DriveService? _service;
    private UserCredential? _credential;
    private bool _disposed;

    /// <summary>
    /// Gets the file operations group (list, find, delete, rename, move, copy).
    /// Reads like the underlying <c>DriveService.Files.*</c> surface.
    /// </summary>
    public IGDriveFileOperations Files { get; }

    /// <summary>
    /// Gets the folder operations group (find, list, list-all, create, delete).
    /// Reads like the underlying <c>DriveService.Files.*</c> surface, scoped to folders.
    /// </summary>
    public IGDriveFolderOperations Folders { get; }

    /// <summary>
    /// Gets the configured root folder ID from options. Default value is "root".
    /// </summary>
    public string RootFolderId => _options.RootFolderId;

    /// <summary>
    /// Gets the configured options for the GoogleDriveApi instance.
    /// </summary>
    public GoogleDriveApiOptions Options => _options;

    private GoogleDriveApi(GoogleDriveApiOptions options, IGoogleDriveAuthProvider authProvider)
    {
        _options = options;
        _authProvider = authProvider;
        Files = new GDriveFileOperations(this);
        Folders = new GDriveFolderOperations(this);
    }

    /// <summary>
    /// Creates a new GoogleDriveApi instance using the provided options and authentication provider.
    /// This method is intended to be called by builders implementing <see cref="IGoogleDriveApiBuilder"/>.
    /// </summary>
    /// <param name="options">The configuration options for the GoogleDriveApi instance.</param>
    /// <param name="authProvider">The authentication provider to use for authorization.</param>
    /// <returns>A new GoogleDriveApi instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="authProvider"/> is null.</exception>
    public static GoogleDriveApi Create(GoogleDriveApiOptions options, IGoogleDriveAuthProvider authProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(authProvider);

        return new(options, authProvider);
    }

    /// <summary>
    /// Gets the underlying <see cref="DriveService"/> for direct access to the Google Drive API.
    /// </summary>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public DriveService Provider
    {
        get
        {
            ThrowIfDisposed();
            return Volatile.Read(ref _service) ?? throw new AuthorizationException("The GoogleDriveApi has not been authorized.");
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="AuthorizeAsync"/> has completed successfully.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public bool IsAuthorized
    {
        get
        {
            ThrowIfDisposed();
            return Volatile.Read(ref _service) is not null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current access token is stale and should be refreshed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public bool IsTokenShouldBeRefreshed
    {
        get
        {
            ThrowIfDisposed();
            return _credential?.Token?.IsStale ?? false;
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="GoogleDriveApi"/> instance.
    /// <para>Documentation: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern</para>
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            Volatile.Read(ref _service)?.Dispose();
            Volatile.Write(ref _service, null);
            _credential = null;
            _authGate.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Creates a builder for configuring a new <see cref="GoogleDriveApi"/> instance.
    /// </summary>
    /// <returns>A new <see cref="GoogleDriveApiBuilder"/> instance.</returns>
    public static IGoogleDriveApiBuilder CreateBuilder() => CreateBuilder<GoogleDriveApiBuilder>();

    /// <summary>
    /// Creates a builder of the specified type for configuring a new <see cref="GoogleDriveApi"/> instance.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type to instantiate.</typeparam>
    /// <returns>A new <typeparamref name="TBuilder"/> instance.</returns>
    public static IGoogleDriveApiBuilder CreateBuilder<TBuilder>() where TBuilder : IGoogleDriveApiBuilder, new()
        => new TBuilder();

    ///<summary>
    /// Authorizes the user in Google Drive using the configured authentication provider.
    /// Authorization is <b>lazy and idempotent</b>: if the client is not yet authorized, the first API
    /// call authorizes it on demand; calling this method when already authorized is a no-op, not an error.
    /// Use <paramref name="cancellationToken"/> to cancel the operation or set a timeout (e.g., <c>new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token</c>).
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="OperationCanceledException">Thrown if the authorization process is cancelled or times out.</exception>
    /// <exception cref="Google.Apis.Auth.OAuth2.Responses.TokenResponseException">Thrown when the OAuth token request fails (e.g., consent declined, invalid credentials, revoked refresh token).</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public async Task AuthorizeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await EnsureServiceAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the authorized <see cref="DriveService"/>, performing the one-time authorization on first
    /// use. Lazy, idempotent and thread-safe: an already-authorized client returns synchronously via the
    /// <c>volatile</c> fast path, and concurrent first-callers authorize exactly once behind the
    /// <see cref="SemaphoreSlim"/> gate.
    /// </summary>
    private async ValueTask<DriveService> EnsureServiceAsync(CancellationToken cancellationToken)
    {
        DriveService? service = Volatile.Read(ref _service);
        if (service is not null)
        {
            return service;
        }

        ThrowIfDisposed();

        await _authGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check: another caller may have authorized while we awaited the gate.
            service = Volatile.Read(ref _service);
            if (service is not null)
            {
                return service;
            }

            ThrowIfDisposed();

            UserCredential credential = await _authProvider.AuthorizeAsync(cancellationToken)
                .ConfigureAwait(false);

            service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _options.ApplicationName,
            });

            _credential = credential;
            Volatile.Write(ref _service, service);
            return service;
        }
        finally
        {
            _authGate.Release();
        }
    }

    /// <inheritdoc/>
    ValueTask<DriveService> IGDriveOperationContext.GetServiceAsync(CancellationToken cancellationToken)
        => EnsureServiceAsync(cancellationToken);

    /// <summary>
    /// Manually refreshes the token if it is stale.
    /// <para>
    /// <b>Note:</b> This method is optional. The Google.Apis library automatically refreshes tokens
    /// before each API request via <see cref="UserCredential.InterceptAsync"/>. Use this method only
    /// if you need to proactively refresh tokens (e.g., before a batch of operations).
    /// </para>
    /// <para>Documentation: https://docs.cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.OAuth2.UserCredential?hl=en#Google_Apis_Auth_OAuth2_UserCredential_RefreshTokenAsync_System_Threading_CancellationToken_</para>
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>True if the token was refreshed, false otherwise.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    /// <exception cref="Google.Apis.Auth.OAuth2.Responses.TokenResponseException">Thrown when the OAuth token refresh fails (e.g., revoked refresh token, invalid credentials).</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_credential is not null && IsTokenShouldBeRefreshed)
        {
            await _credential.RefreshTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Permanently deletes all items from the Google Drive trash.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error (e.g., not found, insufficient permissions, quota exceeded).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        await Provider.Files.EmptyTrash()
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all items currently located in the Google Drive trash.
    /// </summary>
    /// <param name="pageSize">
    /// The maximum number of items to retrieve per page.
    /// Must be greater than zero.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A list of <see cref="GoogleFile"/> objects representing trashed items.
    /// </returns>
    /// <remarks>
    /// Only items marked as trashed are returned.
    /// The method retrieves all available pages until no more results remain.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageSize"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error (e.g., not found, insufficient permissions, quota exceeded).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task<List<GoogleFile>> GetTrashedFilesAsync(int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var trashed = new List<GoogleFile>();

        string? pageToken = null;

        do
        {
            var request = Provider.Files.List();
            request.Q = "trashed = true";
            request.Fields = "nextPageToken, files(id, name, mimeType, parents)";
            request.PageSize = pageSize;
            request.PageToken = pageToken;

            var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

            if (result.Files is not null)
            {
                trashed.AddRange(result.Files);
            }

            pageToken = result.NextPageToken;

        } while (!string.IsNullOrEmpty(pageToken));

        return trashed;
    }

    /// <summary>
    /// Moves a file to the Google Drive trash.
    /// Marks the file as trashed by updating its metadata.
    /// </summary>
    /// <param name="fileId">The ID of the file to move to trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error (e.g., not found, insufficient permissions, quota exceeded).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task MoveFileToTrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        var metadata = new GoogleFile
        {
            Trashed = true
        };

        var updateRequest = Provider.Files.Update(metadata, fileId);
        updateRequest.Fields = "id, trashed";

        await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Restores a file from the Google Drive trash.
    /// Marks the file as not trashed by updating its metadata.
    /// </summary>
    /// <param name="fileId">The ID of the file to restore.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error (e.g., not found, insufficient permissions, quota exceeded).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task RestoreFileFromTrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        var metadata = new GoogleFile
        {
            Trashed = false
        };

        var updateRequest = Provider.Files.Update(metadata, fileId);
        updateRequest.Fields = "id, trashed";

        await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="IGDriveFileOperations.DeleteAsync"/>
    [Obsolete("Use Files.DeleteAsync instead. This forwarder will be removed in v1.")]
    public Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
        => Files.DeleteAsync(fileId, cancellationToken);

    /// <inheritdoc cref="IGDriveFileOperations.RenameAsync"/>
    [Obsolete("Use Files.RenameAsync instead. This forwarder will be removed in v1.")]
    public Task RenameFileAsync(string fileId, string newName, CancellationToken cancellationToken = default)
        => Files.RenameAsync(fileId, newName, cancellationToken);

    /// <inheritdoc cref="IGDriveFileOperations.MoveAsync"/>
    [Obsolete("Use Files.MoveAsync instead. This forwarder will be removed in v1.")]
    public Task MoveFileToAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
        => Files.MoveAsync(fileId, sourceFolderId, destinationFolderId, cancellationToken);

    /// <inheritdoc cref="IGDriveFileOperations.CopyAsync"/>
    [Obsolete("Use Files.CopyAsync instead. This forwarder will be removed in v1.")]
    public Task<string> CopyFileToAsync(string fileId, string destinationFolderId, string? newName = null, CancellationToken cancellationToken = default)
        => Files.CopyAsync(fileId, destinationFolderId, newName, cancellationToken);

    /// <inheritdoc cref="IGDriveFolderOperations.FindIdByNameAsync"/>
    [Obsolete("Use Folders.FindIdByNameAsync instead. This forwarder will be removed in v1.")]
    public Task<string?> GetFolderIdByAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
        => Folders.FindIdByNameAsync(folderName, parentFolderId, cancellationToken);

    /// <inheritdoc cref="IGDriveFolderOperations.ListAsync"/>
    [Obsolete("Use Folders.ListAsync instead. This forwarder will be removed in v1.")]
    public async Task<List<(string id, string name)>> GetFoldersByAsync(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default)
        => (await Folders.ListAsync(parentFolderId, pageSize, cancellationToken).ConfigureAwait(false)).ToList();

    /// <inheritdoc cref="IGDriveFolderOperations.ListAllAsync"/>
    [Obsolete("Use Folders.ListAllAsync instead. This forwarder will be removed in v1.")]
    public async Task<List<GDriveFile>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
        => (await Folders.ListAllAsync(cancellationToken).ConfigureAwait(false)).ToList();

    /// <inheritdoc cref="IGDriveFolderOperations.CreateAsync"/>
    [Obsolete("Use Folders.CreateAsync instead. This forwarder will be removed in v1.")]
    public Task<string> CreateFolderAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
        => Folders.CreateAsync(folderName, parentFolderId, cancellationToken);

    /// <inheritdoc cref="IGDriveFolderOperations.DeleteAsync"/>
    [Obsolete("Use Folders.DeleteAsync instead. This forwarder will be removed in v1.")]
    public Task DeleteFolderAsync(string folderId, CancellationToken cancellationToken = default)
        => Folders.DeleteAsync(folderId, cancellationToken);

    /// <inheritdoc cref="IGDriveFileOperations.FindIdByNameAsync"/>
    [Obsolete("Use Files.FindIdByNameAsync instead. This forwarder will be removed in v1.")]
    public Task<string?> GetFileIdByAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
        => Files.FindIdByNameAsync(fullFileName, parentFolderId, cancellationToken);

    /// <inheritdoc cref="IGDriveFileOperations.ListAsync"/>
    [Obsolete("Use Files.ListAsync instead. This forwarder will be removed in v1.")]
    public async Task<List<GoogleFile>> GetFilesByAsync(string? parentFolderId = null, int pageSize = 100, CancellationToken cancellationToken = default)
        => (await Files.ListAsync(parentFolderId, pageSize, cancellationToken).ConfigureAwait(false)).ToList();

    /// <summary>
    /// Updates the binary content of an existing Google Drive file using a resumable upload.
    /// </summary>
    /// <param name="fileId">The identifier of the file whose content should be updated.</param>
    /// <param name="content">A stream containing the new file content.</param>
    /// <param name="contentType">The MIME type of the content (for example <c>application/pdf</c> or <c>image/png</c>).</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <remarks>
    /// This method replaces the existing file content while preserving the file metadata and validates that the upload completes successfully.
    /// If the provided <paramref name="content"/> stream is seekable, its position is reset before the upload begins.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> or <paramref name="contentType"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="UpdateFileContentException">Thrown when the upload fails or does not complete successfully.</exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public async Task UpdateFileContentAsync(string fileId, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(contentType);

        content.ResetIfSeekable();

        var metadata = new GoogleFile();
        UpdateMediaUpload request = Provider.Files.Update(metadata, fileId, content, contentType);
        request.Fields = "id, md5Checksum, size";

        try
        {
            _ = await UploadAsync(
                request,
                () => null,
                $"Failed to update content for file '{fileId}'.",
                static (message, cause) => new UpdateFileContentException(message, cause),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UpdateFileContentException)
        {
            throw new UpdateFileContentException(
                $"Failed to update content for file '{fileId}'.", ex);
        }
    }

    /// <summary>
    /// Uploads a file from the specified file system path to Google Drive using a resumable upload.
    /// </summary>
    /// <param name="filePath">The full path to the file to be uploaded.</param>
    /// <param name="mimeType">The MIME type of the file content (for example <c>application/pdf</c> or <c>image/png</c>).</param>
    /// <param name="parentFolderId">
    /// Optional ID of the parent folder in which the file will be created.
    /// If <c>null</c>, <see cref="RootFolderId"/> is used.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The identifier of the newly created Google Drive file.</returns>
    /// <remarks>
    /// The file is opened for read-only access and uploaded using a resumable upload.
    /// If the upload completes successfully but no file identifier is returned by the API, an <see cref="UploadFileException"/> is thrown.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> or <paramref name="mimeType"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified by <paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="UploadFileException">Thrown when the upload fails for any reason other than cancellation, including when no valid file identifier is returned.</exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public async Task<string> UploadFilePathAsync(string filePath, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        parentFolderId ??= _options.RootFolderId;

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Cannot find the file at {filePath}.");
        }

        string fileName = Path.GetFileName(filePath);

        // Usage-error checks (authorization, disposal) must run outside the wrapping try.
        FilesResource files = Provider.Files;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var fileMetadata = new GoogleFile
            {
                Name = fileName,
                Parents = [parentFolderId]
            };

            CreateMediaUpload request = files.Create(fileMetadata, stream, mimeType);
            request.Fields = "id";

            GoogleFile result = await UploadAsync(
                    request,
                    () => request.ResponseBody,
                    $"Failed to upload file '{fileName}'.",
                    static (message, cause) => new UploadFileException(message, cause),
                    cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.Id))
                throw new UploadFileException($"Failed to upload file '{fileName}' (no file id returned).");

            return result.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UploadFileException)
        {
            throw new UploadFileException($"Failed to upload file '{fileName}'.", ex);
        }
    }

    /// <summary>
    /// Uploads a file to Google Drive from the provided stream using a resumable upload.
    /// </summary>
    /// <param name="fileStream">A stream containing the file content to be uploaded.</param>
    /// <param name="fileName">The name of the file to be created in Google Drive.</param>
    /// <param name="mimeType">The MIME type of the file content.</param>
    /// <param name="parentFolderId">
    /// Optional ID of the parent folder in which the file will be created.
    /// If <c>null</c>, <see cref="RootFolderId"/> is used.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The identifier of the newly created Google Drive file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileStream"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> or <paramref name="mimeType"/> is null or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="UploadFileException">Thrown when the upload fails for any reason other than cancellation, including when no file identifier is returned.</exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public async Task<string> UploadFileStreamAsync(Stream fileStream, string fileName, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        parentFolderId ??= _options.RootFolderId;

        cancellationToken.ThrowIfCancellationRequested();

        fileStream.ResetIfSeekable();

        var fileMetadata = new GoogleFile()
        {
            Name = fileName,
            Parents = [parentFolderId]
        };

        CreateMediaUpload request = Provider.Files.Create(fileMetadata, fileStream, mimeType);
        request.Fields = "id";

        try
        {
            GoogleFile result = await UploadAsync(
                request,
                () => request.ResponseBody,
                $"Failed to upload file '{fileName}'.",
                static (message, cause) => new UploadFileException(message, cause),
                cancellationToken)
            .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.Id))
                throw new UploadFileException($"Failed to upload file '{fileName}' (no file id returned).");

            return result.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UploadFileException)
        {
            throw new UploadFileException($"Failed to upload file '{fileName}'.", ex);
        }
    }

    /// <summary>
    /// Executes a resumable upload and validates that it completes successfully.
    /// <para>
    /// <see cref="ResumableUpload.UploadAsync(CancellationToken)"/> does not throw on failure —
    /// it returns an <see cref="IUploadProgress"/> with <see cref="UploadStatus.Failed"/> and the error in
    /// <see cref="IUploadProgress.Exception"/>. This method converts that silent failure into an exception
    /// created by <paramref name="exceptionFactory"/>, so each public method surfaces its own documented type.
    /// </para>
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the upload.</typeparam>
    /// <param name="upload">The resumable upload request to execute.</param>
    /// <param name="responseAccessor">A delegate used to retrieve the response after a successful upload.</param>
    /// <param name="errorMessage">The base error message used when the upload fails.</param>
    /// <param name="exceptionFactory">Creates the exception to throw on failure; receives the message and the underlying cause (may be <c>null</c>).</param>
    /// <param name="ct">The token to monitor for cancellation requests.</param>
    /// <param name="onProgress">An optional callback invoked when upload progress changes.</param>
    /// <returns>The response returned by the upload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="upload"/>, <paramref name="responseAccessor"/>, or <paramref name="exceptionFactory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errorMessage"/> is null or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    private static async Task<TResponse> UploadAsync<TResponse>(
        ResumableUpload<TResponse> upload,
        Func<TResponse?> responseAccessor,
        string errorMessage,
        Func<string, Exception?, Exception> exceptionFactory,
        CancellationToken ct = default,
        Action<IUploadProgress>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(responseAccessor);
        ArgumentNullException.ThrowIfNull(exceptionFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        ct.ThrowIfCancellationRequested();

        if (onProgress != null)
            upload.ProgressChanged += onProgress;

        try
        {
            var progress = await upload.UploadAsync(ct).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();

            if (progress.Status == UploadStatus.Failed)
                throw exceptionFactory(errorMessage, progress.Exception);

            if (progress.Status != UploadStatus.Completed)
                throw exceptionFactory($"{errorMessage} Status: {progress.Status}.", null);

            var response = responseAccessor();

            return response!;
        }
        finally
        {
            if (onProgress != null)
                upload.ProgressChanged -= onProgress;
        }
    }

    /// <summary>
    /// Downloads a Google Drive file to the specified local directory.
    /// Google Workspace files (Docs, Sheets, Slides, etc.) are exported to a compatible format;
    /// all other files are downloaded as-is.
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <param name="saveToPath">
    /// The local directory where the downloaded file will be saved.
    /// The directory is created if it does not exist. Defaults to <c>Downloads</c>.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileId"/> or <paramref name="saveToPath"/> is <c>null</c> or empty.</exception>
    /// <exception cref="UnsupportedMimeTypeException">Thrown when the file's MIME type is not supported for download or export.</exception>
    /// <exception cref="DownloadFileException">Thrown when the file cannot be downloaded, exported, or saved locally.</exception>
    /// <exception cref="AuthorizationException">Thrown when the instance has not been authorized. Call <see cref="AuthorizeAsync"/> first.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    /// <exception cref="Google.GoogleApiException">Thrown when the Google Drive API returns an error (e.g., not found, insufficient permissions, quota exceeded).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task DownloadFileAsync(string fileId, string saveToPath = "Downloads", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(saveToPath);

        var request = Provider.Files.Get(fileId);
        GoogleFile file = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        string fileName = PathHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(file.Name));
        string fileMimeType = file.MimeType;

        bool isGoogleSpecificMimeType = GDriveMimeTypes.IsValid(fileMimeType);
        if (isGoogleSpecificMimeType)
        {
            fileMimeType = GDriveMimeTypes.GetExportMimeTypeBy(fileMimeType)
                ?? throw new UnsupportedMimeTypeException(fileId, file.MimeType);
        }

        string extension = MimeTypeHelper.GetExtensionBy(fileMimeType)
            ?? throw new UnsupportedMimeTypeException(fileId, file.MimeType);

        try
        {
            Directory.CreateDirectory(saveToPath);

            string fullPath = Path.Combine(saveToPath, $"{fileName}.{extension}");

            if (isGoogleSpecificMimeType)
            {
                await ExportGoogleFileAsync(fileId, fileMimeType, fullPath, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await DownloadBinaryFileAsync(fileId, fullPath, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not DownloadFileException)
        {
            throw new DownloadFileException($"Failed to download file '{fileId}'.", ex);
        }
    }

    /// <summary>
    /// Exports a Google-specific file (like Google Docs, Sheets, Slides) to a specified MIME type and saves it locally.
    /// </summary>
    private async Task ExportGoogleFileAsync(string fileId, string exportMimeType, string fullFilePath, CancellationToken cancellationToken)
    {
        var request = Provider.Files.Export(fileId, exportMimeType);
        using var streamFile = new MemoryStream();

        request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
        {
            if (progress.Status == DownloadStatus.Downloading)
            {
                Debug.WriteLine($"BytesDownloaded: {progress.BytesDownloaded}");
            }
        };

        IDownloadProgress result = await request.DownloadAsync(streamFile, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Status != DownloadStatus.Completed)
        {
            Debug.WriteLine("Export failed.");
            throw new DownloadFileException("Failed to export the file from Google Drive.", result.Exception);
        }

        streamFile.SaveToFile(fullFilePath);
        Debug.WriteLine("Export complete.");
    }

    /// <summary>
    /// Downloads a binary file from Google Drive and saves it locally.
    /// </summary>
    private async Task DownloadBinaryFileAsync(string fileId, string fullFilePath, CancellationToken cancellationToken)
    {
        var request = Provider.Files.Get(fileId);
        using var streamFile = new MemoryStream();

        request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
        {
            if (progress.Status == DownloadStatus.Downloading)
            {
                Debug.WriteLine($"BytesDownloaded: {progress.BytesDownloaded}");
            }
        };

        IDownloadProgress result = await request.DownloadAsync(streamFile, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Status != DownloadStatus.Completed)
        {
            Debug.WriteLine("Download failed.");
            throw new DownloadFileException("Failed to download the file from Google Drive.", result.Exception);
        }

        streamFile.SaveToFile(fullFilePath);
        Debug.WriteLine("Download complete.");
    }
}
