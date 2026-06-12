using GoogleDriveApi_DotNet;
using MimeMapping;
using GoogleFile = Google.Apis.Drive.v3.Data.File;
using static GDriveExplorerWinForms.Services.OperationLogger;

namespace GDriveExplorerWinForms.Services;

/// <summary>
/// Thin wrapper around <see cref="GoogleDriveApi"/>. All UI code goes through this service so
/// every library call can be logged (method name, arguments, outcome, elapsed time) — the log
/// panel is how the sample teaches which library API each UI action maps to.
/// </summary>
public sealed class DriveExplorerService : IDisposable
{
    private readonly GoogleDriveApi _api;

    public OperationLogger Logger { get; } = new();

    public DriveExplorerService(GoogleDriveApi api)
    {
        _api = api;
    }

    public string RootFolderId => _api.RootFolderId;
    public bool IsAuthorized => _api.IsAuthorized;
    public bool IsTokenStale => _api.IsTokenShouldBeRefreshed;
    public string? ApplicationName => _api.Options.ApplicationName;

    public void Dispose() => _api.Dispose();

    // ---- Folders -------------------------------------------------------

    public Task<List<(string id, string name)>> GetFoldersAsync(string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("GetFoldersByAsync", parentFolderId),
            () => _api.GetFoldersByAsync(parentFolderId, cancellationToken: ct),
            folders => $"{folders.Count} folder(s)");

    public Task<string> CreateFolderAsync(string name, string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("CreateFolderAsync", name, parentFolderId),
            () => _api.CreateFolderAsync(name, parentFolderId, ct),
            id => $"created id {id}");

    public Task DeleteFolderAsync(string folderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("DeleteFolderAsync", folderId),
            () => _api.DeleteFolderAsync(folderId, ct),
            "deleted permanently");

    public Task<string?> FindFolderIdAsync(string name, string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("GetFolderIdByAsync", name, parentFolderId),
            () => _api.GetFolderIdByAsync(name, parentFolderId, ct),
            id => id is null ? "no match" : $"found id {id}");

    // ---- Files ---------------------------------------------------------

    public Task<List<GoogleFile>> GetFilesAsync(string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("GetFilesByAsync", parentFolderId),
            () => _api.GetFilesByAsync(parentFolderId, cancellationToken: ct),
            files => $"{files.Count} file(s)");

    public Task<string> UploadFileAsync(string filePath, string parentFolderId, CancellationToken ct)
    {
        string mimeType = MimeUtility.GetMimeMapping(filePath);
        return Logger.TrackAsync(FormatCall("UploadFilePathAsync", Path.GetFileName(filePath), mimeType, parentFolderId),
            () => _api.UploadFilePathAsync(filePath, mimeType, parentFolderId, ct),
            id => $"uploaded id {id}");
    }

    public async Task<string> UploadFileStreamAsync(string filePath, string parentFolderId, CancellationToken ct)
    {
        string fileName = Path.GetFileName(filePath);
        string mimeType = MimeUtility.GetMimeMapping(filePath);
        await using FileStream stream = File.OpenRead(filePath);
        return await Logger.TrackAsync(FormatCall("UploadFileStreamAsync", new Verbatim("stream"), fileName, mimeType, parentFolderId),
            () => _api.UploadFileStreamAsync(stream, fileName, mimeType, parentFolderId, ct),
            id => $"uploaded id {id}");
    }

    public Task DownloadFileAsync(string fileId, string saveToPath, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("DownloadFileAsync", fileId, saveToPath),
            () => _api.DownloadFileAsync(fileId, saveToPath, ct),
            "download complete");

    public async Task UpdateFileContentAsync(string fileId, string contentFilePath, CancellationToken ct)
    {
        string mimeType = MimeUtility.GetMimeMapping(contentFilePath);
        await using FileStream stream = File.OpenRead(contentFilePath);
        await Logger.TrackAsync(FormatCall("UpdateFileContentAsync", fileId, new Verbatim("stream"), mimeType),
            () => _api.UpdateFileContentAsync(fileId, stream, mimeType, ct),
            "content replaced");
    }

    public Task RenameFileAsync(string fileId, string newName, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("RenameFileAsync", fileId, newName),
            () => _api.RenameFileAsync(fileId, newName, ct),
            "renamed");

    public Task MoveFileAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("MoveFileToAsync", fileId, sourceFolderId, destinationFolderId),
            () => _api.MoveFileToAsync(fileId, sourceFolderId, destinationFolderId, ct),
            "moved");

    public Task<string> CopyFileAsync(string fileId, string destinationFolderId, string? newName, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("CopyFileToAsync", fileId, destinationFolderId, newName),
            () => _api.CopyFileToAsync(fileId, destinationFolderId, newName, ct),
            id => $"copy id {id}");

    public Task<string?> FindFileIdAsync(string name, string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("GetFileIdByAsync", name, parentFolderId),
            () => _api.GetFileIdByAsync(name, parentFolderId, ct),
            id => id is null ? "no match" : $"found id {id}");

    public Task DeleteFileAsync(string fileId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("DeleteFileAsync", fileId),
            () => _api.DeleteFileAsync(fileId, ct),
            "deleted permanently");

    // ---- Trash ---------------------------------------------------------

    public Task TrashFileAsync(string fileId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("MoveFileToTrashAsync", fileId),
            () => _api.MoveFileToTrashAsync(fileId, ct),
            "moved to trash");

    public Task RestoreFileAsync(string fileId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("RestoreFileFromTrashAsync", fileId),
            () => _api.RestoreFileFromTrashAsync(fileId, ct),
            "restored");

    public Task<List<GoogleFile>> GetTrashedFilesAsync(CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("GetTrashedFilesAsync"),
            () => _api.GetTrashedFilesAsync(cancellationToken: ct),
            items => $"{items.Count} item(s) in trash");

    public Task EmptyTrashAsync(CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("EmptyTrashAsync"),
            () => _api.EmptyTrashAsync(ct),
            "trash emptied");

    // ---- Token ---------------------------------------------------------

    public Task<bool> TryRefreshTokenAsync(CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("TryRefreshTokenAsync"),
            () => _api.TryRefreshTokenAsync(ct),
            refreshed => refreshed ? "token refreshed" : "refresh not needed/failed");
}
