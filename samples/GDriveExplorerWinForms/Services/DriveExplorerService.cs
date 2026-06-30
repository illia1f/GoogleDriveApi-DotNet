using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Types;
using MimeMapping;
using GoogleFile = Google.Apis.Drive.v3.Data.File;
using static GDriveExplorerWinForms.Services.OperationLogger;

namespace GDriveExplorerWinForms.Services;

/// <summary>
/// Thin wrapper around <see cref="GoogleDriveApi"/>. All UI code goes through this service so
/// every library call can be logged (method name, arguments, outcome, elapsed time) — the log
/// panel is how the sample teaches which library API each UI action maps to. The logged labels
/// match the operation-group surface (<c>Files</c>, <c>Folders</c>, <c>Transfers</c>, <c>Trash</c>).
/// </summary>
public sealed class DriveExplorerService(GoogleDriveApi api) : IDisposable
{
    private readonly GoogleDriveApi _api = api;

    public OperationLogger Logger { get; } = new();

    public string RootFolderId => _api.RootFolderId;
    public bool IsAuthorized => _api.IsAuthorized;
    public bool IsTokenStale => _api.IsTokenShouldBeRefreshed;
    public string? ApplicationName => _api.Options.ApplicationName;

    public void Dispose() => _api.Dispose();

    // ---- Folders -------------------------------------------------------

    public Task<IReadOnlyList<DriveItem>> GetFoldersAsync(string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Folders.ListAsync", parentFolderId),
            () => _api.Folders.ListAsync(parentFolderId, cancellationToken: ct),
            folders => $"{folders.Count} folder(s)");

    public Task<string> CreateFolderAsync(string name, string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Folders.CreateAsync", name, parentFolderId),
            () => _api.Folders.CreateAsync(name, parentFolderId, ct),
            id => $"created id {id}");

    public Task DeleteFolderAsync(string folderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Folders.DeleteAsync", folderId),
            () => _api.Folders.DeleteAsync(folderId, ct),
            "deleted permanently");

    public Task RenameFolderAsync(string folderId, string newName, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Folders.RenameAsync", folderId, newName),
            () => _api.Folders.RenameAsync(folderId, newName, ct),
            "renamed");

    public Task MoveFolderAsync(string folderId, string sourceFolderId, string destinationFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Folders.MoveAsync", folderId, sourceFolderId, destinationFolderId),
            () => _api.Folders.MoveAsync(folderId, sourceFolderId, destinationFolderId, ct),
            "moved");

    public Task<DriveItem?> FindFolderAsync(string name, string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Folders.FindFirstByNameAsync", name, parentFolderId),
            () => _api.Folders.FindFirstByNameAsync(name, parentFolderId, ct),
            folder => folder is null ? "no match" : $"found id {folder.Id}");

    // ---- Files ---------------------------------------------------------

    public Task<IReadOnlyList<GoogleFile>> GetFilesAsync(string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Files.ListAsync", parentFolderId, new Verbatim("Default.WithSize().WithModifiedTime()")),
            // The list view shows Size and Modified columns, so request exactly those extra fields.
            () => _api.Files.ListAsync(parentFolderId, DriveFields.Default.WithSize().WithModifiedTime(), cancellationToken: ct),
            files => $"{files.Count} file(s)");

    public Task<string> UploadFileAsync(string filePath, string parentFolderId, CancellationToken ct)
    {
        string mimeType = MimeUtility.GetMimeMapping(filePath);
        return Logger.TrackAsync(FormatCall("Transfers.UploadAsync", Path.GetFileName(filePath), mimeType, parentFolderId),
            () => _api.Transfers.UploadAsync(filePath, mimeType, parentFolderId, ct),
            id => $"uploaded id {id}");
    }

    public async Task<string> UploadFileStreamAsync(string filePath, string parentFolderId, CancellationToken ct)
    {
        string fileName = Path.GetFileName(filePath);
        string mimeType = MimeUtility.GetMimeMapping(filePath);
        await using FileStream stream = File.OpenRead(filePath);
        return await Logger.TrackAsync(FormatCall("Transfers.UploadAsync", new Verbatim("stream"), fileName, mimeType, parentFolderId),
            () => _api.Transfers.UploadAsync(stream, fileName, mimeType, parentFolderId, ct),
            id => $"uploaded id {id}");
    }

    public Task DownloadFileAsync(string fileId, string saveToPath, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Transfers.DownloadAsync", fileId, saveToPath),
            () => _api.Transfers.DownloadAsync(fileId, saveToPath, ct),
            "download complete");

    public async Task UpdateFileContentAsync(string fileId, string contentFilePath, CancellationToken ct)
    {
        string mimeType = MimeUtility.GetMimeMapping(contentFilePath);
        await using FileStream stream = File.OpenRead(contentFilePath);
        await Logger.TrackAsync(FormatCall("Transfers.UpdateContentAsync", fileId, new Verbatim("stream"), mimeType),
            () => _api.Transfers.UpdateContentAsync(fileId, stream, mimeType, ct),
            "content replaced");
    }

    public Task RenameFileAsync(string fileId, string newName, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Files.RenameAsync", fileId, newName),
            () => _api.Files.RenameAsync(fileId, newName, ct),
            "renamed");

    public Task MoveFileAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Files.MoveAsync", fileId, sourceFolderId, destinationFolderId),
            () => _api.Files.MoveAsync(fileId, sourceFolderId, destinationFolderId, ct),
            "moved");

    public Task<string> CopyFileAsync(string fileId, string destinationFolderId, string? newName, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Files.CopyAsync", fileId, destinationFolderId, newName),
            () => _api.Files.CopyAsync(fileId, destinationFolderId, newName, ct),
            id => $"copy id {id}");

    public Task<DriveItem?> FindFileAsync(string name, string parentFolderId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Files.FindFirstByNameAsync", name, parentFolderId),
            () => _api.Files.FindFirstByNameAsync(name, parentFolderId, ct),
            file => file is null ? "no match" : $"found id {file.Id}");

    public Task DeleteFileAsync(string fileId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Files.DeleteAsync", fileId),
            () => _api.Files.DeleteAsync(fileId, ct),
            "deleted permanently");

    // ---- Trash ---------------------------------------------------------

    public Task TrashFileAsync(string fileId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Trash.TrashAsync", fileId),
            () => _api.Trash.TrashAsync(fileId, ct),
            "moved to trash");

    public Task RestoreFileAsync(string fileId, CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Trash.RestoreAsync", fileId),
            () => _api.Trash.RestoreAsync(fileId, ct),
            "restored");

    public Task<IReadOnlyList<DriveItem>> GetTrashedFilesAsync(CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Trash.ListAsync"),
            () => _api.Trash.ListAsync(cancellationToken: ct),
            items => $"{items.Count} item(s) in trash");

    public Task EmptyTrashAsync(CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("Trash.EmptyAsync"),
            () => _api.Trash.EmptyAsync(ct),
            "trash emptied");

    // ---- Token ---------------------------------------------------------

    public Task<bool> TryRefreshTokenAsync(CancellationToken ct) =>
        Logger.TrackAsync(FormatCall("TryRefreshTokenAsync"),
            () => _api.TryRefreshTokenAsync(ct),
            refreshed => refreshed ? "token refreshed" : "refresh not needed/failed");
}
