using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Operations;

/// <inheritdoc cref="IGDriveFileOperations"/>
internal sealed class GDriveFileOperations(IGDriveOperationContext context) : IGDriveFileOperations
{
    private readonly IGDriveOperationContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DriveItem>> ListAsync(string? parentFolderId = null, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GoogleFile> files = await ListAsync(parentFolderId, DriveFields.Default, pageSize, cancellationToken).ConfigureAwait(false);
        return files.Select(f => f.ToDriveItem()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoogleFile>> ListAsync(string? parentFolderId, DriveFields fields, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        parentFolderId ??= _context.RootFolderId;

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        string query = $"mimeType != '{MimeType.Folder}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed = false";
        return await service.ListAsync(query, fields.ToListMask(), pageSize, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<GoogleFile?> FindByIdAsync(string fileId, DriveFields fields, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentNullException.ThrowIfNull(fields);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var request = service.Files.Get(fileId);
        request.Fields = fields.ToGetMask();

        return await request
            .WithDefaultOnNotFound()
            .ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> FindIdByNameAsync(string fullFileName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullFileName);

        parentFolderId ??= _context.RootFolderId;

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var request = service.Files.List();
        request.Q = $"mimeType != '{MimeType.Folder}' and name = '{DriveQueryHelper.EscapeValue(fullFileName)}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed = false";
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

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(fileId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFile();

        await service.Files
            .Delete(fileId)
            .ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RenameAsync(string fileId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(fileId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFile();

        var metadata = new GoogleFile { Name = newName };

        var updateRequest = service.Files.Update(metadata, fileId);
        updateRequest.Fields = "id,name";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MoveAsync(string fileId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        ArgumentException.ThrowIfNullOrEmpty(sourceFolderId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(fileId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFile();

        var metadata = new GoogleFile();

        var updateRequest = service.Files.Update(metadata, fileId);
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

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(fileId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFile();

        var metadata = new GoogleFile
        {
            Name = string.IsNullOrWhiteSpace(newName) ? null : newName,
            Parents = [destinationFolderId]
        };

        var copyRequest = service.Files.Copy(metadata, fileId);
        copyRequest.Fields = "id, name, parents";

        GoogleFile copiedFile = await copyRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return copiedFile.Id;
    }
}
