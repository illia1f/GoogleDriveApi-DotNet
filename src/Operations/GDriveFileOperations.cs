using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Operations;

/// <inheritdoc cref="IGDriveFileOperations"/>
internal sealed class GDriveFileOperations : IGDriveFileOperations
{
    private readonly IGDriveOperationContext _context;

    internal GDriveFileOperations(IGDriveOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoogleFile>> ListAsync(string? parentFolderId = null, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        parentFolderId ??= _context.RootFolderId;

        var files = new List<GoogleFile>();
        string? pageToken = null;
        string qSelector = $"mimeType != '{GDriveMimeTypes.Folder}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed = false";
        const string fields = "nextPageToken, files(id, name, mimeType, size, modifiedTime)";
        do
        {
            var listRequest = _context.Provider.Files.List();
            listRequest.Q = qSelector;
            listRequest.Fields = fields;
            listRequest.PageSize = pageSize;
            listRequest.PageToken = pageToken;

            var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (result.Files is not null)
            {
                files.AddRange(result.Files);
            }

            pageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return files;
    }

    /// <inheritdoc/>
    public async Task<string?> FindIdByNameAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullFileName);

        parentFolderId ??= _context.RootFolderId;

        var request = _context.Provider.Files.List();
        request.Q = $"name = '{DriveQueryHelper.EscapeValue(fullFileName)}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed = false";
        request.Fields = "files(id, name)";
        request.PageSize = 1;

        var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        var file = result.Files?.FirstOrDefault();

        return file?.Id;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        GoogleFile file = await _context.Provider.Files.Get(fileId)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (file.MimeType == GDriveMimeTypes.Folder)
        {
            throw new InvalidMimeTypeException(fileId, file.MimeType);
        }

        await _context.Provider.Files.Delete(fileId)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RenameAsync(string fileId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var metadata = new GoogleFile { Name = newName };

        var updateRequest = _context.Provider.Files.Update(metadata, fileId);
        updateRequest.Fields = "id,name";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MoveAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(sourceFolderId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var metadata = new GoogleFile();

        var updateRequest = _context.Provider.Files.Update(metadata, fileId);
        updateRequest.AddParents = destinationFolderId;
        updateRequest.RemoveParents = sourceFolderId;
        updateRequest.Fields = "id, parents";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> CopyAsync(string fileId, string destinationFolderId, string? newName = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var metadata = new GoogleFile
        {
            Name = string.IsNullOrWhiteSpace(newName) ? null : newName,
            Parents = [destinationFolderId]
        };

        var copyRequest = _context.Provider.Files.Copy(metadata, fileId);
        copyRequest.Fields = "id, name, parents";

        GoogleFile copiedFile = await copyRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return copiedFile.Id;
    }
}
