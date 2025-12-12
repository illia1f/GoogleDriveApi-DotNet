using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Models;
using System.Diagnostics;

namespace GoogleDriveApi_DotNet;

public class GoogleDriveApi
{
    public const int AuthorizationTimeOutInSec = 30;
    public const string RootFolderId = "root";
    private readonly string _credentialsPath;
    private readonly string _tokenFolderPath;
    private readonly string? _applicationName;
    private DriveService? _service;
    private UserCredential? _credential;

    public DriveService Provider => _service ?? throw new AuthorizationException("The GoogleDriveApi has not been authorized.");
    public bool IsAuthorized => _service is not null;
    public bool IsTokenShouldBeRefreshed => _credential?.Token?.IsStale ?? false;

    private GoogleDriveApi(string credentialsPath, string tokenFolderPath, string applicationName)
    {
        _credentialsPath = credentialsPath;
        _tokenFolderPath = tokenFolderPath;
        _applicationName = applicationName;
    }

    public static GoogleDriveApiBuilder CreateBuilder() => new GoogleDriveApiBuilder();

    public class GoogleDriveApiBuilder
    {
        private string _credentialsPath = "credentials.json";
        private string _tokenFolderPath = "_metadata";
        private string _applicationName = "[Your App Name]";

        /// <summary>
        /// Sets the path to the credentials JSON file. Default value is "credentials.json".
        /// **Note**: Place the downloaded JSON file in your project directory.
        /// <para>Documentation: https://developers.google.com/identity/protocols/oauth2</para>
        /// </summary>
        /// <param name="path">The path to the credentials JSON file.</param>
        /// <returns>The builder instance.</returns>
        public GoogleDriveApiBuilder SetCredentialsPath(string path)
        {
            _credentialsPath = path;
            return this;
        }

        /// <summary>
        /// Sets the path to the token JSON file folder where it will store the token in a file. Default value is "_metadata".
        /// <para>Documentation: https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth</para>
        /// </summary>
        /// <param name="folderPath">The path to the token JSON file.</param>
        /// <returns>The builder instance.</returns>
        public GoogleDriveApiBuilder SetTokenFolderPath(string folderPath)
        {
            _tokenFolderPath = folderPath;
            return this;
        }

        /// <summary>
        /// Sets the name of the application. Default value is null.
        /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Services.BaseClientService.Initializer#Google_Apis_Services_BaseClientService_Initializer_ApplicationName</para>
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <returns>The builder instance.</returns>
        public GoogleDriveApiBuilder SetApplicationName(string name)
        {
            _applicationName = name;
            return this;
        }

        ///<inheritdoc cref="Internal_BuildAsync"/>
        public GoogleDriveApi Build(bool immediateAuthorization = true, int timeOutInSec = AuthorizationTimeOutInSec)
        {
            return Internal_BuildAsync(immediateAuthorization, timeOutInSec)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        ///<inheritdoc cref="Internal_BuildAsync"/>
        public async Task<GoogleDriveApi> BuildAsync(bool immediateAuthorization = true, int timeOutInSec = AuthorizationTimeOutInSec)
        {
            return await Internal_BuildAsync(immediateAuthorization, timeOutInSec)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Builds the GoogleDriveApi instance and attempts to authorize if <paramref name="immediateAuthorization"/> is true within <paramref name="timeOutInSec"/> seconds. 
        /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker?hl=en#Google_Apis_Auth_OAuth2_GoogleWebAuthorizationBroker_AuthorizeAsync_Google_Apis_Auth_OAuth2_ClientSecrets_System_Collections_Generic_IEnumerable_System_String__System_String_System_Threading_CancellationToken_Google_Apis_Util_Store_IDataStore_Google_Apis_Auth_OAuth2_ICodeReceiver_</para>
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The result contains the authorized GoogleDriveApi instance.</returns>
        /// <inheritdoc cref="Internal_AuthorizeAsync"/>
        private async Task<GoogleDriveApi> Internal_BuildAsync(bool immediateAuthorization, int timeOutInSec)
        {
            var gDriveApi = new GoogleDriveApi(_credentialsPath, _tokenFolderPath, _applicationName);

            if (immediateAuthorization)
            {
                await gDriveApi.Internal_AuthorizeAsync(timeOutInSec)
                    .ConfigureAwait(false);
            }

            return gDriveApi;
        }
    }

    /// <inheritdoc cref="Internal_AuthorizeAsync"/>
    public void Authorize(int timeOutInSec = AuthorizationTimeOutInSec)
    {
        Internal_AuthorizeAsync(timeOutInSec)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_AuthorizeAsync"/>
    public async Task AuthorizeAsync(int timeOutInSec = AuthorizationTimeOutInSec)
    {
        await Internal_AuthorizeAsync(timeOutInSec)
            .ConfigureAwait(false);
    }

    ///<summary>
    /// Authorizes the user in Google Drive.
    /// </summary>
    /// <exception cref="OperationCanceledException">Thrown if the authorization process times out.</exception>
    private async Task Internal_AuthorizeAsync(int timeOutInSec)
    {
        if (IsAuthorized)
        {
            throw new AuthorizationException("The GoogleDriveApi has been already authorized.");
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeOutInSec));

        using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
        {
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { DriveService.Scope.Drive },
                "user",
                cts.Token,
                new FileDataStore(_tokenFolderPath, true))
                .ConfigureAwait(false);
        }

        _service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = _credential,
            ApplicationName = _applicationName,
        });
    }

    /// <inheritdoc cref="Internal_TryRefreshTokenAsync"/>
    public bool TryRefreshToken()
    {
        return Internal_TryRefreshTokenAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_TryRefreshTokenAsync"/>
    public Task<bool> TryRefreshTokenAsync()
    {
        return Internal_TryRefreshTokenAsync();
    }

    /// <summary>
    /// Refreshes the token by calling to RefreshTokenAsync.
    /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.OAuth2.UserCredential?hl=en#Google_Apis_Auth_OAuth2_UserCredential_RefreshTokenAsync_System_Threading_CancellationToken_</para>
    /// </summary>
    private async Task<bool> Internal_TryRefreshTokenAsync()
    {
        if (_credential is not null && IsTokenShouldBeRefreshed)
        {
            await _credential.RefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="Internal_GetFolderIdByAsync"/>
    public string? GetFolderIdBy(string folderName, string parentFolderId = RootFolderId)
    {
        TryRefreshToken();

        return Internal_GetFolderIdByAsync(folderName, parentFolderId)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetFolderIdByAsync"/>
    public async Task<string?> GetFolderIdByAsync(string folderName, string parentFolderId = RootFolderId)
    {
        await TryRefreshTokenAsync().ConfigureAwait(false);

        return await Internal_GetFolderIdByAsync(folderName, parentFolderId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the ID of a folder by its name within a specified parent folder. 
    /// Default value  for <paramref name="parentFolderId"/> is "root".
    /// </summary>
    /// <param name="folderName">The name of the folder to search for.</param>
    /// <param name="parentFolderId">(optional) The ID of the parent folder to search within (default is "root").</param>
    /// <returns>The ID of the folder if found; otherwise, null.</returns>
    private async Task<string?> Internal_GetFolderIdByAsync(string folderName, string parentFolderId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(folderName);
        ArgumentNullException.ThrowIfNullOrEmpty(parentFolderId);

        var listRequest = Provider.Files.List();
        listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and '{parentFolderId}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 1;

        var result = await listRequest.ExecuteAsync().ConfigureAwait(false);
        GoogleFile? file = result.Files.FirstOrDefault();

        return file?.Id;
    }

    /// <inheritdoc cref="Internal_GetFoldersByAsync"/>
    public List<(string id, string name)> GetFoldersBy(string parentFolderId, int pageSize = 50)
    {
        TryRefreshToken();

        return Internal_GetFoldersByAsync(parentFolderId, pageSize)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetFoldersByAsync"/>
    public async Task<List<(string id, string name)>> GetFoldersByAsync(string parentFolderId, int pageSize = 50)
    {
        await TryRefreshTokenAsync().ConfigureAwait(false);

        return await Internal_GetFoldersByAsync(parentFolderId, pageSize)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a list of folders within a specified parent folder.
    /// </summary>
    /// <param name="parentFolderId">The ID of the parent folder to search within.</param>
    /// <param name="pageSize">(optional) The number of results to retrieve per page (default is 50).</param>
    /// <returns>A list of tuples, each containing the ID and name of a folder.</returns>
    private async Task<List<(string id, string name)>> Internal_GetFoldersByAsync(string parentFolderId, int pageSize)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(parentFolderId);

        if (pageSize <= 0)
        {
            throw new ArgumentException("PageSize cannot be smaller than 1.");
        }

        var allFolders = new List<GoogleFile>();
        string? pageToken = null;
        string qSelector = $"mimeType='application/vnd.google-apps.folder' and '{parentFolderId}' in parents and trashed=false";
        const string fields = "nextPageToken, files(id, name)";
        do
        {
            var listRequest = Provider.Files.List();
            listRequest.Q = qSelector;
            listRequest.Fields = fields;
            listRequest.PageSize = pageSize;
            listRequest.PageToken = pageToken;

            var result = await listRequest.ExecuteAsync().ConfigureAwait(false);
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
    public List<GDriveFile> GetAllFolders()
    {
        return Internal_GetAllFoldersAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetAllFoldersAsync"/>
    public async Task<List<GDriveFile>> GetAllFoldersAsync()
    {
        return await Internal_GetAllFoldersAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all folders from Google Drive.
    /// This method sends multiple requests to ensure all folders are retrieved, with each request
    /// fetching up to 1000 folders (the maximum page size).
    /// </summary>
    /// <returns>A list of GDriveFile objects representing all folders in the Google Drive.</returns>
    private async Task<List<GDriveFile>> Internal_GetAllFoldersAsync()
    {
        var request = Provider.Files.List();
        request.Q = "mimeType = 'application/vnd.google-apps.folder'";
        request.Fields = "nextPageToken, files(id, name, parents)";
        request.PageSize = 1000; // Set page size to maximum (1000)

        var folders = new List<GoogleFile>();
        do
        {
            var result = await request.ExecuteAsync().ConfigureAwait(false);
            folders.AddRange(result.Files);
            request.PageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(request.PageToken));

        var driveFolders = folders.Select(f => f.ToGDriveFile()).ToList();

        return driveFolders;
    }

    /// <inheritdoc cref="Internal_CreateFolderAsync"/>
    public string CreateFolder(string folderName, string parentFolderId = RootFolderId)
    {
        TryRefreshToken();

        return Internal_CreateFolderAsync(folderName, parentFolderId)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_CreateFolderAsync"/>
    public async Task<string> CreateFolderAsync(string folderName, string parentFolderId = RootFolderId)
    {
        await TryRefreshTokenAsync().ConfigureAwait(false);

        return await Internal_CreateFolderAsync(folderName, parentFolderId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new folder in Google Drive.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <param name="parentFolderId">(optional) The ID of the parent folder where the new folder will be created (default is "root").</param>
    /// <returns>The ID of the created folder.</returns>
    private async Task<string> Internal_CreateFolderAsync(string folderName, string parentFolderId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(folderName);
        ArgumentNullException.ThrowIfNullOrEmpty(parentFolderId);

        var driveFolder = new GoogleFile()
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new[] { parentFolderId }
        };

        var request = Provider.Files.Create(driveFolder);
        GoogleFile file = await request.ExecuteAsync().ConfigureAwait(false);

        return file.Id;
    }

    /// <inheritdoc cref="Internal_DeleteFolderAsync"/>
    public bool DeleteFolder(string folderId)
    {
        return Internal_DeleteFolderAsync(folderId)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_DeleteFolderAsync"/>
    public async Task<bool> DeleteFolderAsync(string folderId)
    {
        await TryRefreshTokenAsync().ConfigureAwait(false);

        return await Internal_DeleteFolderAsync(folderId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a folder from Google Drive using its folder ID.
    /// </summary>
    /// <param name="folderId">The ID of the folder to be removed.</param>
    /// <returns> The task result is a boolean indicating success or failure.</returns>
    private async Task<bool> Internal_DeleteFolderAsync(string folderId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(folderId);

        GoogleFile folder = await Provider.Files.Get(folderId).ExecuteAsync().ConfigureAwait(false);
        if (folder.MimeType != "application/vnd.google-apps.folder")
        {
            Console.WriteLine("The specified ID does not correspond to a folder.");
            return false;
        }

        await Provider.Files.Delete(folderId).ExecuteAsync().ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc cref="Internal_GetFileIdByAsync"/>
    public string? GetFileIdBy(string fullFileName, string parentFolderId = RootFolderId)
    {
        TryRefreshToken();

        return Internal_GetFileIdByAsync(fullFileName, parentFolderId)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc cref="Internal_GetFileIdByAsync"/>
    public async Task<string?> GetFileIdByAsync(string fullFileName, string parentFolderId = RootFolderId)
    {
        await TryRefreshTokenAsync().ConfigureAwait(false);

        return await Internal_GetFileIdByAsync(fullFileName, parentFolderId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the file ID by its name and parent folder ID.
    /// </summary>
    /// <param name="fullFileName">The name of the file with an extension to search for.</param>
    /// <param name="parentFolderId">The ID of the parent folder where the file is located. Use "root" for the root directory.</param>
    /// <returns>The file ID if found, otherwise null.</returns>
    /// <exception cref="AuthorizationException">Thrown if the GoogleDriveApi is not initialized and authorized.</exception>
    private async Task<string?> Internal_GetFileIdByAsync(string fullFileName, string parentFolderId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fullFileName);
        ArgumentNullException.ThrowIfNullOrEmpty(parentFolderId);

        var request = Provider.Files.List();
        request.Q = $"name = '{fullFileName}' and '{parentFolderId}' in parents and trashed = false";
        request.Fields = "files(id, name)";
        request.PageSize = 1;

        var result = await request.ExecuteAsync().ConfigureAwait(false);
        var file = result.Files.FirstOrDefault();

        return file?.Id;
    }

    /// <summary>
    /// Uploads a file to Google Drive using a file path.
    /// </summary>
    /// <param name="filePath">The path of the file to be uploaded.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    public string UploadFilePath(string filePath, string mimeType)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Cannot find the file at {filePath}.");
        }

        string fileName = Path.GetFileName(filePath);
        using var stream = new FileStream(filePath, FileMode.Open);

        return Internal_UploadFileStream(stream, fileName, mimeType);
    }

    /// <inheritdoc cref="Internal_UploadFileStream"/>
    public string UploadFileStream(Stream fileStream, string fileName, string mimeType)
    {
        return Internal_UploadFileStream(fileStream, fileName, mimeType);
    }

    /// <summary>
    /// Uploads a file to Google Drive using a Stream.
    /// </summary>
    /// <param name="fileStream">The file stream to be uploaded.</param>
    /// <param name="fileName">The name of the file on Google Drive.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <exception cref="CreateMediaUploadException">Thrown if the file upload failed.</exception>
    private string Internal_UploadFileStream(Stream fileStream, string fileName, string mimeType)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNullOrEmpty(mimeType);

        var fileMetadata = new GoogleFile()
        {
            Name = fileName
        };

        FilesResource.CreateMediaUpload request =
            Provider.Files.Create(fileMetadata, fileStream, mimeType);

        request.Fields = "id";

        var uploadProgress = request.Upload();

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
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="AuthorizationException">Thrown if the GoogleDriveApi is not initialized and authorized.</exception>
    /// <inheritdoc cref="ExportGoogleFileAsync"/>
    /// <inheritdoc cref="DownloadBinaryFileAsync"/>
    public async Task DownloadFileAsync(string fileId, string saveToPath = "Downloads")
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fileId);
        ArgumentNullException.ThrowIfNullOrEmpty(saveToPath);

        await TryRefreshTokenAsync().ConfigureAwait(false);

        // Ensure the folderPath directory exists
        Directory.CreateDirectory(saveToPath);

        var request = Provider.Files.Get(fileId);
        GoogleFile file = await request.ExecuteAsync().ConfigureAwait(false);
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        string? fileMimeType = file.MimeType;

        // Check for a specific Google Workplace Mime Types
        bool isGoogleSpecificMimeType = MimeTypeHelper.IsGDriveMimeType(fileMimeType);
        if (isGoogleSpecificMimeType)
        {
            fileMimeType = MimeTypeHelper.GetExportMimeTypeBy(fileMimeType);
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
            await ExportGoogleFileAsync(fileId, fileMimeType, fullPath).ConfigureAwait(false);
        }
        else
        {
            await DownloadBinaryFileAsync(fileId, fullPath).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Exports a Google-specific file (like Google Docs, Sheets, Slides) to a specified MIME type.
    /// </summary>
    /// <param name="fileId">The ID of the file to export.</param>
    /// <param name="exportMimeType">The MIME type to which the file should be exported.</param>
    /// <param name="fullFilePath">The full path where the exported file will be saved.</param>
    /// <exception cref="ExportRequestException">Thrown if the export failed.</exception>
    private async Task ExportGoogleFileAsync(string fileId, string exportMimeType, string fullFilePath)
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

        await request.DownloadAsync(streamFile).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a binary file from Google Drive.
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <param name="fullFilePath">The full path where the downloaded file will be saved.</param>
    /// <exception cref="GetRequestException">Thrown if the downloading failed.</exception>
    private async Task DownloadBinaryFileAsync(string fileId, string fullFilePath)
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

        await request.DownloadAsync(streamFile).ConfigureAwait(false);
    }

    private static void SaveStream(MemoryStream stream, string fullFilePath)
    {
        using var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write);

        stream.WriteTo(fileStream);
    }
}