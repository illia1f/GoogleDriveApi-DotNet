using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;
using System.Diagnostics;
using System.Threading;

namespace GoogleDriveApi_DotNet;

public class GoogleDriveApi : IDisposable
{
    private readonly GoogleDriveApiOptions _options;
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
    private GoogleDriveApi(GoogleDriveApiOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Creates a new GoogleDriveApi instance using the provided options. This method is intended to be called by builders implementing <see cref="IGoogleDriveApiBuilder"/>.
    /// </summary>
    /// <param name="options">The configuration options for the GoogleDriveApi instance.</param>
    /// <returns>A new GoogleDriveApi instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public static GoogleDriveApi Create(GoogleDriveApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new(options);
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

    /// <inheritdoc cref="Internal_AuthorizeAsync"/>
    public void Authorize(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Internal_AuthorizeAsync(cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_AuthorizeAsync"/>
    public async Task AuthorizeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Internal_AuthorizeAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    ///<summary>
    /// Authorizes the user in Google Drive.
    /// Use <paramref name="cancellationToken"/> to cancel the operation or set a timeout (e.g., <c>new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token</c>).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation or set a timeout.</param>
    /// <exception cref="OperationCanceledException">Thrown if the authorization process is cancelled or times out.</exception>
    internal async Task Internal_AuthorizeAsync(CancellationToken cancellationToken)
    {
        if (IsAuthorized)
        {
            throw new AuthorizationException("The GoogleDriveApi has been already authorized.");
        }

        using (var stream = new FileStream(_options.CredentialsPath, FileMode.Open, FileAccess.Read))
        {
            var gcSecrets = await GoogleClientSecrets.FromStreamAsync(stream);
            var dataStore = new FileDataStore(_options.TokenFolderPath, fullPath: true);
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets: gcSecrets.Secrets,
                scopes: [DriveService.Scope.Drive],
                user: _options.UserId,
                cancellationToken,
                dataStore).ConfigureAwait(false);
        }

        _service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = _credential,
            ApplicationName = _options.ApplicationName,
        });
    }

    /// <inheritdoc cref="Internal_TryRefreshTokenAsync"/>
    public bool TryRefreshToken(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Internal_TryRefreshTokenAsync(cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_TryRefreshTokenAsync"/>
    public Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Internal_TryRefreshTokenAsync(cancellationToken);
    }

    /// <summary>
    /// Refreshes the token by calling to RefreshTokenAsync.
    /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.OAuth2.UserCredential?hl=en#Google_Apis_Auth_OAuth2_UserCredential_RefreshTokenAsync_System_Threading_CancellationToken_</para>
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
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

    /// <inheritdoc cref="Internal_GetFolderIdByAsync"/>
    public string? GetFolderIdBy(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        TryRefreshToken(cancellationToken);

        return Internal_GetFolderIdByAsync(folderName, parentFolderId, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetFolderIdByAsync"/>
    public async Task<string?> GetFolderIdByAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetFolderIdByAsync(folderName, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Internal_RenameFileAsync"/>
    public async Task RenameFileAsync(string fileId, string newName, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        await Internal_RenameFileAsync(fileId, newName, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Internal_MoveFileToAsync"/>
    public async Task MoveFileToAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        await Internal_MoveFileToAsync(fileId, sourceFolderId, destinationFolderId, cancellationToken);
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
    /// The filename and all other metadata remain unchanged.
    /// </remarks>
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

    /// <summary>
    /// Retrieves the ID of a folder by its name within a specified parent folder. 
    /// Default value  for <paramref name="parentFolderId"/> is <see cref="RootFolderId"/>.
    /// </summary>
    /// <param name="folderName">The name of the folder to search for.</param>
    /// <param name="parentFolderId">(optional) The ID of the parent folder to search within (default is "root").</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the folder if found; otherwise, null.</returns>
    private async Task<string?> Internal_GetFolderIdByAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _options.RootFolderId;

        var listRequest = Provider.Files.List();
        listRequest.Q = $"mimeType='{GDriveMimeTypes.Folder}' and name='{folderName}' and '{parentFolderId}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 1;

        var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        GoogleFile? file = result.Files.FirstOrDefault();

        return file?.Id;
    }

    /// <inheritdoc cref="Internal_GetFoldersByAsync"/>
    public List<(string id, string name)> GetFoldersBy(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        TryRefreshToken(cancellationToken);

        return Internal_GetFoldersByAsync(parentFolderId, pageSize, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetFoldersByAsync"/>
    public async Task<List<(string id, string name)>> GetFoldersByAsync(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetFoldersByAsync(parentFolderId, pageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a list of folders within a specified parent folder.
    /// </summary>
    /// <param name="parentFolderId">The ID of the parent folder to search within.</param>
    /// <param name="pageSize">(optional) The number of results to retrieve per page (default is 50).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of tuples, each containing the ID and name of a folder.</returns>
    private async Task<List<(string id, string name)>> Internal_GetFoldersByAsync(string parentFolderId, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(parentFolderId);

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

    /// <inheritdoc cref="Internal_GetAllFoldersAsync"/>
    public List<GDriveFile> GetAllFolders(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Internal_GetAllFoldersAsync(cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetAllFoldersAsync"/>
    public async Task<List<GDriveFile>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await Internal_GetAllFoldersAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all folders from Google Drive.
    /// This method sends multiple requests to ensure all folders are retrieved, with each request
    /// fetching up to 1000 folders (the maximum page size).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of GDriveFile objects representing all folders in the Google Drive.</returns>
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

    /// <inheritdoc cref="Internal_CreateFolderAsync"/>
    public string CreateFolder(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        TryRefreshToken(cancellationToken);

        return Internal_CreateFolderAsync(folderName, parentFolderId, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_CreateFolderAsync"/>
    public async Task<string> CreateFolderAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_CreateFolderAsync(folderName, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new folder in Google Drive.
    /// Default value for <paramref name="parentFolderId"/> is <see cref="RootFolderId"/>.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <param name="parentFolderId">(optional) The ID of the parent folder where the new folder will be created (default is "root").</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the created folder.</returns>
    private async Task<string> Internal_CreateFolderAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(folderName);

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
    public bool DeleteFolder(string folderId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Internal_DeleteFolderAsync(folderId, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_DeleteFolderAsync"/>
    public async Task<bool> DeleteFolderAsync(string folderId, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_DeleteFolderAsync(folderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a folder from Google Drive using its folder ID.
    /// </summary>
    /// <param name="folderId">The ID of the folder to be removed.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns> The task result is a boolean indicating success or failure.</returns>
    private async Task<bool> Internal_DeleteFolderAsync(string folderId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(folderId);

        GoogleFile folder = await Provider.Files.Get(folderId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (folder.MimeType != GDriveMimeTypes.Folder)
        {
            Console.WriteLine("The specified ID does not correspond to a folder.");
            return false;
        }

        await Provider.Files.Delete(folderId).ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc cref="Internal_GetFileIdByAsync"/>
    public string? GetFileIdBy(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        TryRefreshToken(cancellationToken);

        return Internal_GetFileIdByAsync(fullFileName, parentFolderId, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetFileIdByAsync"/>
    public async Task<string?> GetFileIdByAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

        return await Internal_GetFileIdByAsync(fullFileName, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the file ID by its name and parent folder ID.
    /// Default value for <paramref name="parentFolderId"/> is <see cref="RootFolderId"/>.
    /// </summary>
    /// <param name="fullFileName">The name of the file with an extension to search for.</param>
    /// <param name="parentFolderId">The ID of the parent folder where the file is located. Use "root" for the root directory.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The file ID if found, otherwise null.</returns>
    /// <exception cref="AuthorizationException">Thrown if the GoogleDriveApi is not initialized and authorized.</exception>
    private async Task<string?> Internal_GetFileIdByAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fullFileName);

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
        ArgumentNullException.ThrowIfNullOrEmpty(filePath);
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
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNullOrEmpty(mimeType);

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
        ArgumentNullException.ThrowIfNullOrEmpty(fileId);
        ArgumentNullException.ThrowIfNullOrEmpty(saveToPath);

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