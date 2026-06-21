using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Operations;
using GoogleDriveApi_DotNet.Types;

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
    /// Gets the transfer operations group (upload, update content, download with Workspace export).
    /// </summary>
    public IGDriveTransferOperations Transfers { get; }

    /// <summary>
    /// Gets the trash operations group (move to trash, restore, empty, list trashed items).
    /// </summary>
    public IGDriveTrashOperations Trash { get; }

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
        Transfers = new GDriveTransferOperations(this);
        Trash = new GDriveTrashOperations(this);
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

    /// <inheritdoc cref="IGDriveTrashOperations.EmptyAsync"/>
    [Obsolete("Use Trash.EmptyAsync instead. This forwarder will be removed in v1.")]
    public Task EmptyTrashAsync(CancellationToken cancellationToken = default)
        => Trash.EmptyAsync(cancellationToken);

    /// <inheritdoc cref="IGDriveTrashOperations.ListAsync"/>
    [Obsolete("Use Trash.ListAsync instead. This forwarder will be removed in v1.")]
    public async Task<List<GoogleFile>> GetTrashedFilesAsync(int pageSize = 50, CancellationToken cancellationToken = default)
        => (await Trash.ListAsync(pageSize, cancellationToken).ConfigureAwait(false)).ToList();

    /// <inheritdoc cref="IGDriveTrashOperations.TrashAsync"/>
    [Obsolete("Use Trash.TrashAsync instead. This forwarder will be removed in v1.")]
    public Task MoveFileToTrashAsync(string fileId, CancellationToken cancellationToken = default)
        => Trash.TrashAsync(fileId, cancellationToken);

    /// <inheritdoc cref="IGDriveTrashOperations.RestoreAsync"/>
    [Obsolete("Use Trash.RestoreAsync instead. This forwarder will be removed in v1.")]
    public Task RestoreFileFromTrashAsync(string fileId, CancellationToken cancellationToken = default)
        => Trash.RestoreAsync(fileId, cancellationToken);

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

    /// <inheritdoc cref="IGDriveTransferOperations.UpdateContentAsync"/>
    [Obsolete("Use Transfers.UpdateContentAsync instead. This forwarder will be removed in v1.")]
    public Task UpdateFileContentAsync(string fileId, Stream content, string contentType, CancellationToken cancellationToken = default)
        => Transfers.UpdateContentAsync(fileId, content, contentType, cancellationToken);

    /// <inheritdoc cref="IGDriveTransferOperations.UploadAsync(string, string, string?, CancellationToken)"/>
    [Obsolete("Use Transfers.UploadAsync instead. This forwarder will be removed in v1.")]
    public Task<string> UploadFilePathAsync(string filePath, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default)
        => Transfers.UploadAsync(filePath, mimeType, parentFolderId, cancellationToken);

    /// <inheritdoc cref="IGDriveTransferOperations.UploadAsync(Stream, string, string, string?, CancellationToken)"/>
    [Obsolete("Use Transfers.UploadAsync instead. This forwarder will be removed in v1.")]
    public Task<string> UploadFileStreamAsync(Stream fileStream, string fileName, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default)
        => Transfers.UploadAsync(fileStream, fileName, mimeType, parentFolderId, cancellationToken);

    /// <inheritdoc cref="IGDriveTransferOperations.DownloadAsync"/>
    [Obsolete("Use Transfers.DownloadAsync instead. This forwarder will be removed in v1.")]
    public Task DownloadFileAsync(string fileId, string saveToPath = "Downloads", CancellationToken cancellationToken = default)
        => Transfers.DownloadAsync(fileId, saveToPath, cancellationToken);
}
