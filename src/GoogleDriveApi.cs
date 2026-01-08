using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;
using System.Diagnostics;

namespace GoogleDriveApi_DotNet;

public class GoogleDriveApi : IDisposable
{
    private readonly GoogleDriveApiOptions _options;
    private readonly IGoogleDriveAuthProvider _authProvider;
    private DriveService? _service;
    private UserCredential? _credential;
    private bool _disposed;

    /// <summary>
    /// Gets the configured root folder ID from options. Default value is "root".
    /// </summary>
    public string RootFolderId => _options.RootFolderId;

    /// <summary>
    /// Gets the configured options for the GoogleDriveApi instance.
    /// </summary>
    public GoogleDriveApiOptions Options => _options;

    /// <summary>
    /// Private constructor to prevent direct instantiation. Use <see cref="Create"/> method instead.
    /// </summary>
    private GoogleDriveApi(GoogleDriveApiOptions options, IGoogleDriveAuthProvider authProvider)
    {
        _options = options;
        _authProvider = authProvider;
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

    public DriveService Provider
    {
        get
        {
            ThrowIfDisposed();
            return _service ?? throw new AuthorizationException("The GoogleDriveApi has not been authorized.");
        }
    }

    public bool IsAuthorized
    {
        get
        {
            ThrowIfDisposed();
            return _service is not null;
        }
    }
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
            _service?.Dispose();
            _service = null;
            _credential = null;
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
    /// </summary>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public static IGoogleDriveApiBuilder CreateBuilder() => CreateBuilder<GoogleDriveApiBuilder>();

    public static IGoogleDriveApiBuilder CreateBuilder<TBuilder>() where TBuilder : IGoogleDriveApiBuilder, new()
        => new TBuilder();

    ///<summary>
    /// Authorizes the user in Google Drive using the configured authentication provider.
    /// Use <paramref name="cancellationToken"/> to cancel the operation or set a timeout (e.g., <c>new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token</c>).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation or set a timeout.</param>
    /// <exception cref="OperationCanceledException">Thrown if the authorization process is cancelled or times out.</exception>
    /// <exception cref="AuthorizationException">Thrown if already authorized.</exception>
    public async Task AuthorizeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Internal_AuthorizeAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task Internal_AuthorizeAsync(CancellationToken cancellationToken)
    {
        if (IsAuthorized)
        {
            throw new AuthorizationException("The GoogleDriveApi has been already authorized.");
        }

        _credential = await _authProvider.AuthorizeAsync(cancellationToken)
            .ConfigureAwait(false);

        _service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = _credential,
            ApplicationName = _options.ApplicationName,
        });
    }

    /// <summary>
    /// Refreshes the token if it is stale.
    /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.OAuth2.UserCredential?hl=en#Google_Apis_Auth_OAuth2_UserCredential_RefreshTokenAsync_System_Threading_CancellationToken_</para>
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the token was refreshed, false otherwise.</returns>
    public Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Internal_TryRefreshTokenAsync(cancellationToken);
    }

    private async Task<bool> Internal_TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (_credential is not null && IsTokenShouldBeRefreshed)
        {
            await _credential.RefreshTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="Internal_EmptyTrashAsync"/>
    public async Task EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        await Internal_EmptyTrashAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Permanently deletes all items from the Google Drive trash.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task Internal_EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        await Provider.Files.EmptyTrash()
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Internal_GetTrashedFilesAsync(int, CancellationToken)"/>
    public async Task<List<GoogleFile>> GetTrashedFilesAsync(int pageSize = 50, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        return await Internal_GetTrashedFilesAsync(pageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all items currently located in the Google Drive trash.
    /// </summary>
    /// <param name="pageSize">
    /// The maximum number of items to retrieve per page.
    /// Must be greater than zero.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="GoogleFile"/> objects representing trashed items.
    /// </returns>
    /// <remarks>
    /// Only items marked as trashed are returned.
    /// The method retrieves all available pages until no more results remain.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pageSize"/> is less than or equal to zero.
    /// </exception>
    private async Task<List<GoogleFile>> Internal_GetTrashedFilesAsync(int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentException("PageSize cannot be smaller than 1.");
        }

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

    /// <inheritdoc cref="Internal_MoveFileToTrashAsync"/>
    public async Task MoveFileToTrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        await Internal_MoveFileToTrashAsync(fileId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Moves a file to the Google Drive trash.
    /// Marks the file as trashed by updating its metadata.
    /// </summary>
    /// <param name="fileId">The ID of the file to move to trash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="TrashFileException">
    /// Thrown when the file could not be moved to the Google Drive trash.
    /// </exception>
    private async Task Internal_MoveFileToTrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        var metadata = new GoogleFile
        {
            Trashed = true
        };

        var updateRequest = Provider.Files.Update(metadata, fileId);
        updateRequest.Fields = "id, trashed";

        var updated = await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (updated is null || updated.Trashed != true)
        {
            throw new TrashFileException($"Failed to move file '{fileId}' to trash.");
        }
    }

    /// <inheritdoc cref="Internal_RestoreFileFromTrashAsync"/>
    public async Task RestoreFileFromTrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        await Internal_RestoreFileFromTrashAsync(fileId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Restores a file from the Google Drive trash.
    /// Marks the file as not trashed by updating its metadata.
    /// </summary>
    /// <param name="fileId">The ID of the file to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="RestoreFileException">
    /// Thrown when the file could not be restored from the Google Drive trash.
    /// </exception>
    private async Task Internal_RestoreFileFromTrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        var metadata = new GoogleFile
        {
            Trashed = false
        };

        var updateRequest = Provider.Files.Update(metadata, fileId);
        updateRequest.Fields = "id, trashed";

        var updated = await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (updated is null || updated.Trashed != false)
        {
            throw new RestoreFileException($"Failed to restore file '{fileId}' from trash.");
        }
    }

    /// <inheritdoc cref="Internal_DeleteFileAsync"/>
    public async Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        await Internal_DeleteFileAsync(fileId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a file from Google Drive.
    /// Retrieves the file metadata to validate its type
    /// and then permanently deletes the file.
    /// </summary>
    /// <param name="fileId">The ID of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the file was successfully deleted.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="InvalidFileTypeException">
    /// Thrown when the specified ID refers to an item
    /// that is not a file (for example, a folder).
    /// </exception>
    private async Task Internal_DeleteFileAsync(string fileId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        GoogleFile file = await Provider.Files.Get(fileId)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (file.MimeType == GDriveMimeTypes.Folder)
        {
            throw new InvalidFileTypeException(fileId, file.MimeType);
        }

        await Provider.Files.Delete(fileId)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Internal_RenameFileAsync"/>
    public async Task RenameFileAsync(string fileId, string newName, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        await Internal_RenameFileAsync(fileId, newName, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Renames a file in Google Drive by updating its metadata.
    /// </summary>
    /// <param name="fileId">The ID of the file to rename.</param>
    /// <param name="newName">The new name to assign to the file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <remarks>
    /// This method performs a partial update of the file metadata.
    /// Only the <c>Name</c> field is modified; all other properties remain unchanged.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> or <paramref name="newName"/> is <c>null</c> or empty.
    /// </exception>
    private async Task Internal_RenameFileAsync(
        string fileId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var metadata = new GoogleFile { Name = newName };

        var updateRequest = Provider.Files.Update(metadata, fileId);
        updateRequest.Fields = "id,name";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Internal_MoveFileToAsync"/>
    public async Task MoveFileToAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        await Internal_MoveFileToAsync(fileId, sourceFolderId, destinationFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Moves a file to another folder in Google Drive by updating its parent references.
    /// </summary>
    /// <param name="fileId">The ID of the file to move.</param>
    /// <param name="sourceFolderId">The ID of the folder from which the file will be moved.</param>
    /// <param name="destinationFolderId">The ID of the folder to which the file will be moved.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <remarks>
    /// This method performs a partial update of the file metadata in Google Drive.
    /// Only the file's parent folders are modified — the file is removed from
    /// <paramref name="sourceFolderId"/> and added to <paramref name="destinationFolderId"/>.
    /// The file name and all other metadata remain unchanged.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/>, <paramref name="sourceFolderId"/>,
    /// or <paramref name="destinationFolderId"/> is <c>null</c> or empty.
    /// </exception>
    private async Task Internal_MoveFileToAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(sourceFolderId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var metadata = new GoogleFile();

        var updateRequest = Provider.Files.Update(metadata, fileId);

        updateRequest.AddParents = destinationFolderId;

        updateRequest.RemoveParents = sourceFolderId;

        updateRequest.Fields = "id, parents";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Internal_CopyFileToAsync"/>
    public async Task<string> CopyFileToAsync(string fileId, string destinationFolderId, string? newName = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        return await Internal_CopyFileToAsync(fileId, destinationFolderId, newName, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Copies a file in Google Drive to the specified destination folder.
    /// </summary>
    /// <param name="fileId">The ID of the file to copy.</param>
    /// <param name="destinationFolderId">The ID of the folder where the copied file will be placed.</param>
    /// <param name="newName">
    /// Optional new name for the copied file. If <c>null</c> or empty,
    /// the original file name is preserved.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The ID of the newly created copied file.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileId"/> or <paramref name="destinationFolderId"/> is
    /// <c>null</c> or empty.
    /// </exception>
    /// <exception cref="CopyFileException">
    /// Thrown when the file could not be copied to the specified destination folder.
    /// </exception>
    private async Task<string> Internal_CopyFileToAsync(string fileId, string destinationFolderId, string? newName = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var metadata = new GoogleFile
        {
            Name = string.IsNullOrWhiteSpace(newName) ? null : newName,
            Parents = new[] { destinationFolderId }
        };

        var copyRequest = Provider.Files.Copy(metadata, fileId);
        copyRequest.Fields = "id, name, parents";

        var copiedFile = await copyRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (copiedFile is null)
        {
            throw new CopyFileException($"Failed to copy file '{fileId}' to folder '{destinationFolderId}'.");
        }

        return copiedFile.Id;
    }

    /// <inheritdoc cref="Internal_GetFolderIdByAsync(string, string?, CancellationToken)"/>
    public async Task<string?> GetFolderIdByAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetFolderIdByAsync(folderName, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the ID of a folder by its name within the specified parent folder.
    /// </summary>
    /// <param name="folderName">The name of the folder to search for.</param>
    /// <param name="parentFolderId">
    /// Optional ID of the parent folder to search within. If <c>null</c>,
    /// <see cref="_options"/>.<c>RootFolderId</c> is used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The folder ID if a matching folder is found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// The search is limited to non-trashed items and returns at most one result.
    /// If multiple folders with the same name exist in the same parent folder,
    /// the returned folder is unspecified.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="folderName"/> is <c>null</c> or empty.
    /// </exception>
    private async Task<string?> Internal_GetFolderIdByAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _options.RootFolderId;

        var listRequest = Provider.Files.List();
        listRequest.Q = $"mimeType='{GDriveMimeTypes.Folder}' and name='{folderName}' and '{parentFolderId}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 1;

        var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        GoogleFile? file = result.Files.FirstOrDefault();

        return file?.Id;
    }

    /// <inheritdoc cref="Internal_GetFoldersByAsync(string, int, CancellationToken)"/>
    public async Task<List<(string id, string name)>> GetFoldersByAsync(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetFoldersByAsync(parentFolderId, pageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a list of folders within the specified parent folder.
    /// </summary>
    /// <param name="parentFolderId">The ID of the parent folder to search within.</param>
    /// <param name="pageSize">
    /// The maximum number of folders to retrieve per page.
    /// Must be greater than zero.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of tuples containing the ID and name of each folder.
    /// </returns>
    /// <remarks>
    /// Only non-trashed folders are returned.
    /// The method retrieves all available pages until no more results remain.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="parentFolderId"/> is <c>null</c> or empty,
    /// or when <paramref name="pageSize"/> is less than or equal to zero.
    /// </exception>
    private async Task<List<(string id, string name)>> Internal_GetFoldersByAsync(string parentFolderId, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentFolderId);

        if (pageSize <= 0)
        {
            throw new ArgumentException("PageSize cannot be smaller than 1.");
        }

        var allFolders = new List<GoogleFile>();
        string? pageToken = null;
        string qSelector = $"mimeType='{GDriveMimeTypes.Folder}' and '{parentFolderId}' in parents and trashed=false";
        const string fields = "nextPageToken, files(id, name)";
        do
        {
            var listRequest = Provider.Files.List();
            listRequest.Q = qSelector;
            listRequest.Fields = fields;
            listRequest.PageSize = pageSize;
            listRequest.PageToken = pageToken;

            var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (result.Files is not null)
            {
                allFolders.AddRange(result.Files);
            }

            pageToken = result.NextPageToken;
        } while (pageToken is not null);

        return allFolders
            .Select(f => (f.Id, f.Name))
            .ToList();
    }

    /// <inheritdoc cref="Internal_GetAllFoldersAsync(CancellationToken)"/>
    public async Task<List<GDriveFile>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetAllFoldersAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all folders from Google Drive.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="GDriveFile"/> objects representing all folders in Google Drive.
    /// </returns>
    /// <remarks>
    /// This method retrieves folders using paginated requests.
    /// Each request uses the maximum supported page size (1000) and continues until all pages are fetched.
    /// </remarks>
    private async Task<List<GDriveFile>> Internal_GetAllFoldersAsync(CancellationToken cancellationToken)
    {
        var request = Provider.Files.List();
        request.Q = $"mimeType = '{GDriveMimeTypes.Folder}'";
        request.Fields = "nextPageToken, files(id, name, parents)";
        request.PageSize = 1000; // Set page size to maximum (1000)

        var folders = new List<GoogleFile>();
        do
        {
            var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            folders.AddRange(result.Files);
            request.PageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(request.PageToken));

        var driveFolders = folders.Select(f => f.ToGDriveFile()).ToList();

        return driveFolders;
    }

    /// <inheritdoc cref="Internal_CreateFolderAsync(string, string?, CancellationToken)"/>
    public async Task<string> CreateFolderAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_CreateFolderAsync(folderName, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new folder in Google Drive.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <param name="parentFolderId">
    /// Optional ID of the parent folder in which the new folder will be created.
    /// If <c>null</c>, the root folder is used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The ID of the newly created folder.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="folderName"/> is <c>null</c> or empty.
    /// </exception>
    /// <remarks>
    /// The folder is created using a single Google Drive API request.
    /// Only folder-specific metadata is set; no additional properties are modified.
    /// </remarks>
    private async Task<string> Internal_CreateFolderAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _options.RootFolderId;

        var driveFolder = new GoogleFile()
        {
            Name = folderName,
            MimeType = GDriveMimeTypes.Folder,
            Parents = [parentFolderId]
        };

        var request = Provider.Files.Create(driveFolder);
        GoogleFile file = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return file.Id;
    }

    /// <inheritdoc cref="Internal_DeleteFolderAsync"/>
    public async Task<bool> DeleteFolderAsync(string folderId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_DeleteFolderAsync(folderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a folder from Google Drive after validating its type.
    /// </summary>
    /// <param name="folderId">The ID of the folder to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the folder was successfully deleted.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="folderId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="InvalidFileTypeException">
    /// Thrown when the specified ID refers to an item that is not a folder.
    /// </exception>
    private async Task<bool> Internal_DeleteFolderAsync(string folderId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);

        GoogleFile folder = await Provider.Files.Get(folderId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (folder.MimeType != GDriveMimeTypes.Folder)
        {
            throw new InvalidFileTypeException(folderId, folder.MimeType);
        }

        await Provider.Files.Delete(folderId).ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc cref="Internal_GetFileIdByAsync(string, string?, CancellationToken)"/>
    public async Task<string?> GetFileIdByAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetFileIdByAsync(fullFileName, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the file ID by its name within the specified parent folder.
    /// </summary>
    /// <param name="fullFileName">The file name (including extension) to search for.</param>
    /// <param name="parentFolderId">
    /// Optional ID of the parent folder to search within. If <c>null</c>,
    /// <see cref="_options"/>.<c>RootFolderId</c> is used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The file ID if a matching file is found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// The search is limited to non-trashed items and returns at most one result.
    /// If multiple files with the same name exist in the same parent folder,
    /// the returned file is unspecified.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fullFileName"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="AuthorizationException">
    /// Thrown if the Google Drive API is not initialized or authorized.
    /// </exception>
    private async Task<string?> Internal_GetFileIdByAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullFileName);

        parentFolderId ??= _options.RootFolderId;

        var request = Provider.Files.List();
        request.Q = $"name = '{fullFileName}' and '{parentFolderId}' in parents and trashed = false";
        request.Fields = "files(id, name)";
        request.PageSize = 1;

        var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        var file = result.Files.FirstOrDefault();

        return file?.Id;
    }

    /// <summary>
    /// Uploads a file to Google Drive using a file path.
    /// </summary>
    /// <param name="filePath">The path of the file to be uploaded.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public string UploadFilePath(string filePath, string mimeType, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Cannot find the file at {filePath}.");
        }

        string fileName = Path.GetFileName(filePath);
        using var stream = new FileStream(filePath, FileMode.Open);

        return Internal_UploadFileStream(stream, fileName, mimeType, cancellationToken);
    }

    /// <inheritdoc cref="Internal_UploadFileStream"/>
    public string UploadFileStream(Stream fileStream, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Internal_UploadFileStream(fileStream, fileName, mimeType, cancellationToken);
    }

    /// <summary>
    /// Uploads a file to Google Drive using a Stream.
    /// Note: The Google API's Upload() method does not directly support CancellationToken, but cancellation will be checked before and after the upload operation.
    /// </summary>
    /// <param name="fileStream">The file stream to be uploaded.</param>
    /// <param name="fileName">The name of the file on Google Drive.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <exception cref="CreateMediaUploadException">Thrown if the file upload failed.</exception>
    private string Internal_UploadFileStream(Stream fileStream, string fileName, string mimeType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        cancellationToken.ThrowIfCancellationRequested();

        var fileMetadata = new GoogleFile()
        {
            Name = fileName
        };

        FilesResource.CreateMediaUpload request =
            Provider.Files.Create(fileMetadata, fileStream, mimeType);

        request.Fields = "id";

        var uploadProgress = request.Upload();

        cancellationToken.ThrowIfCancellationRequested();

        if (uploadProgress.Status == Google.Apis.Upload.UploadStatus.Failed)
        {
            Debug.WriteLine("File upload failed.");
            throw new CreateMediaUploadException("File upload failed", uploadProgress.Exception);
        }

        GoogleFile file = request.ResponseBody;

        if (file is null)
        {
            Debug.WriteLine("File upload failed, no response body received.");
            throw new CreateMediaUploadException("File upload failed, no response body received.");
        }

        return file.Id;
    }

    /// <summary>
    /// Downloads a file from Google Drive by its file ID.
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <param name="saveToPath">The local path where the file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="AuthorizationException">Thrown if the GoogleDriveApi is not initialized and authorized.</exception>
    /// <inheritdoc cref="ExportGoogleFileAsync"/>
    /// <inheritdoc cref="DownloadBinaryFileAsync"/>
    public async Task DownloadFileAsync(string fileId, string saveToPath = "Downloads", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(saveToPath);

        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        // Ensure the folderPath directory exists
        Directory.CreateDirectory(saveToPath);

        var request = Provider.Files.Get(fileId);
        GoogleFile file = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        string? fileMimeType = file.MimeType;

        // Check for a specific Google Workplace Mime Types
        bool isGoogleSpecificMimeType = GDriveMimeTypes.IsValid(fileMimeType);
        if (isGoogleSpecificMimeType)
        {
            fileMimeType = GDriveMimeTypes.GetExportMimeTypeBy(fileMimeType);
            if (fileMimeType is null)
            {
                throw new InvalidOperationException($"Unsupported mime type ({file.MimeType})");
            }
        }

        string? extension = MimeTypeHelper.GetExtensionBy(fileMimeType);
        if (extension is null)
        {
            throw new InvalidOperationException($"Unsupported mime type ({file.MimeType})");
        }

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

    /// <summary>
    /// Exports a Google-specific file (like Google Docs, Sheets, Slides) to a specified MIME type.
    /// </summary>
    /// <param name="fileId">The ID of the file to export.</param>
    /// <param name="exportMimeType">The MIME type to which the file should be exported.</param>
    /// <param name="fullFilePath">The full path where the exported file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <exception cref="ExportRequestException">Thrown if the export failed.</exception>
    private async Task ExportGoogleFileAsync(string fileId, string exportMimeType, string fullFilePath, CancellationToken cancellationToken)
    {
        var request = Provider.Files.Export(fileId, exportMimeType);
        var streamFile = new MemoryStream();

        request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Debug.WriteLine($"BytesDownloaded: {progress.BytesDownloaded}");
                    break;
                case DownloadStatus.Completed:
                    SaveStream(streamFile, fullFilePath);
                    Debug.WriteLine("Export complete.");
                    break;
                case DownloadStatus.Failed:
                    Debug.WriteLine("Export failed.");
                    throw new ExportRequestException("Failed to export the file from Google Drive.", progress.Exception);
            }
        };

        await request.DownloadAsync(streamFile, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a binary file from Google Drive.
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <param name="fullFilePath">The full path where the downloaded file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <exception cref="GetRequestException">Thrown if the downloading failed.</exception>
    private async Task DownloadBinaryFileAsync(string fileId, string fullFilePath, CancellationToken cancellationToken)
    {
        var request = Provider.Files.Get(fileId);
        var streamFile = new MemoryStream();

        request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Debug.WriteLine($"BytesDownloaded: {progress.BytesDownloaded}");
                    break;
                case DownloadStatus.Completed:
                    SaveStream(streamFile, fullFilePath);
                    Debug.WriteLine("Download complete.");
                    break;
                case DownloadStatus.Failed:
                    Debug.WriteLine("Download failed.");
                    throw new GetRequestException("Failed to download the file from Google Drive.", progress.Exception);
            }
        };

        await request.DownloadAsync(streamFile, cancellationToken).ConfigureAwait(false);
    }

    private static void SaveStream(MemoryStream stream, string fullFilePath)
    {
        using var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write);

        stream.WriteTo(fileStream);
    }
}
