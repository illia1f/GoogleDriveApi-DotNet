using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;
using System.Diagnostics;
using static Google.Apis.Drive.v3.FilesResource;

namespace GoogleDriveApi_DotNet.Operations;

/// <inheritdoc cref="IDriveTransfers"/>
internal sealed class DriveTransfers(IDriveOperationContext context) : IDriveTransfers
{
    private readonly IDriveOperationContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<string> UploadAsync(string filePath, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        parentFolderId ??= _context.RootFolderId;

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Cannot find the file at {filePath}.");
        }

        string fileName = Path.GetFileName(filePath);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);
        FilesResource files = service.Files;

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

            GoogleFile result = await ExecuteUploadAsync(
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

    /// <inheritdoc/>
    public async Task<string> UploadAsync(Stream content, string fileName, string mimeType, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        parentFolderId ??= _context.RootFolderId;

        cancellationToken.ThrowIfCancellationRequested();

        content.ResetIfSeekable();

        var fileMetadata = new GoogleFile()
        {
            Name = fileName,
            Parents = [parentFolderId]
        };

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        CreateMediaUpload request = service.Files.Create(fileMetadata, content, mimeType);
        request.Fields = "id";

        try
        {
            GoogleFile result = await ExecuteUploadAsync(
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

    /// <inheritdoc/>
    public async Task UpdateContentAsync(string fileId, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(contentType);

        content.ResetIfSeekable();

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var metadata = new GoogleFile();
        UpdateMediaUpload request = service.Files.Update(metadata, fileId, content, contentType);
        request.Fields = "id, md5Checksum, size";

        try
        {
            _ = await ExecuteUploadAsync(
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

    /// <inheritdoc/>
    public async Task DownloadAsync(string fileId, string saveToPath = "Downloads", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(saveToPath);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var request = service.Files.Get(fileId);
        GoogleFile file = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        string fileName = PathHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(file.Name));

        MimeType mimeType = MimeType.Create(file.MimeType);
        bool isGoogleSpecificMimeType = mimeType.IsGoogleWorkspace;

        MimeType effectiveMimeType = isGoogleSpecificMimeType
            ? mimeType.GetExportMimeType() ?? throw new UnsupportedMimeTypeException(fileId, file.MimeType)
            : mimeType;

        string extension = MimeTypeHelper.GetExtensionBy(effectiveMimeType.Value)
            ?? throw new UnsupportedMimeTypeException(fileId, file.MimeType);

        try
        {
            Directory.CreateDirectory(saveToPath);

            string fullPath = Path.Combine(saveToPath, $"{fileName}.{extension}");

            if (isGoogleSpecificMimeType)
            {
                await ExportGoogleFileAsync(service, fileId, effectiveMimeType.Value, fullPath, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await DownloadBinaryFileAsync(service, fileId, fullPath, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not DownloadFileException)
        {
            throw new DownloadFileException($"Failed to download file '{fileId}'.", ex);
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
    private static async Task<TResponse> ExecuteUploadAsync<TResponse>(
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
    /// Exports a Google-specific file (like Google Docs, Sheets, Slides) to a specified MIME type and saves it locally.
    /// </summary>
    private static async Task ExportGoogleFileAsync(DriveService service, string fileId, string exportMimeType, string fullFilePath, CancellationToken cancellationToken)
    {
        var request = service.Files.Export(fileId, exportMimeType);
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
    private static async Task DownloadBinaryFileAsync(DriveService service, string fileId, string fullFilePath, CancellationToken cancellationToken)
    {
        var request = service.Files.Get(fileId);
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
