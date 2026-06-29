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
public class GoogleDriveApi : IDisposable, IDriveOperationContext
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
    public IDriveFiles Files { get; }

    /// <summary>
    /// Gets the folder operations group (find, list, list-all, create, delete, rename, move).
    /// Reads like the underlying <c>DriveService.Files.*</c> surface, scoped to folders.
    /// </summary>
    public IDriveFolders Folders { get; }

    /// <summary>
    /// Gets the transfer operations group (upload, update content, download with Workspace export).
    /// </summary>
    public IDriveTransfers Transfers { get; }

    /// <summary>
    /// Gets the trash operations group (move to trash, restore, empty, list trashed items).
    /// </summary>
    public IDriveTrash Trash { get; }

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
        Files = new DriveFiles(this);
        Folders = new DriveFolders(this);
        Transfers = new DriveTransfers(this);
        Trash = new DriveTrash(this);
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
    ValueTask<DriveService> IDriveOperationContext.GetServiceAsync(CancellationToken cancellationToken)
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
}
