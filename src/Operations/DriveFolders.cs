using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Operations;

/// <inheritdoc cref="IDriveFolders"/>
internal sealed class DriveFolders(IDriveOperationContext context) : IDriveFolders
{
    private readonly IDriveOperationContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<string?> FindIdByNameAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _context.RootFolderId;

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);

        var listRequest = service.Files.List();
        listRequest.Q = $"mimeType='{MimeType.Folder}' and name='{DriveQueryHelper.EscapeValue(folderName)}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 1;

        var result = await listRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        GoogleFile? file = result.Files?.FirstOrDefault();

        return file?.Id;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DriveItem>> ListAsync(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GoogleFile> folders = await ListAsync(parentFolderId, DriveFields.Default, pageSize, cancellationToken).ConfigureAwait(false);
        return folders.Select(f => f.ToDriveItem()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoogleFile>> ListAsync(string parentFolderId, DriveFields fields, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentFolderId);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);

        string query = $"mimeType='{MimeType.Folder}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed=false";

        return await service
            .ListAsync(query, fields.ToListMask(), pageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DriveItem>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GoogleFile> folders = await ListAllAsync(DriveFields.Default, cancellationToken).ConfigureAwait(false);
        return folders.Select(f => f.ToDriveItem()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoogleFile>> ListAllAsync(DriveFields fields, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);
        
        string query = $"mimeType = '{MimeType.Folder}'";

        // Maximum supported page size (1000), so the full set is fetched in as few requests as possible.
        return await service
            .ListAsync(query, fields.ToListMask(), pageSize: 1000, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> CreateAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _context.RootFolderId;

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);

        var driveFolder = new GoogleFile()
        {
            Name = folderName,
            MimeType = MimeType.Folder,
            Parents = [parentFolderId]
        };

        var request = service.Files.Create(driveFolder);
        GoogleFile file = await request
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return file.Id;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string folderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);

        MimeType mimeType = await service
            .GetMimeTypeAsync(folderId, cancellationToken)
            .ConfigureAwait(false);
        mimeType.RequireFolder();

        await service.Files
            .Delete(folderId)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RenameAsync(string folderId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);

        MimeType mimeType = await service
            .GetMimeTypeAsync(folderId, cancellationToken)
            .ConfigureAwait(false);
        mimeType.RequireFolder();

        var metadata = new GoogleFile { Name = newName };

        var updateRequest = service.Files.Update(metadata, folderId);
        updateRequest.Fields = "id,name";

        await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MoveAsync(string folderId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);
        ArgumentException.ThrowIfNullOrEmpty(sourceFolderId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var service = await _context
            .GetServiceAsync(cancellationToken)
            .ConfigureAwait(false);

        MimeType mimeType = await service
            .GetMimeTypeAsync(folderId, cancellationToken)
            .ConfigureAwait(false);
        mimeType.RequireFolder();

        var metadata = new GoogleFile();

        var updateRequest = service.Files.Update(metadata, folderId);
        updateRequest.AddParents = destinationFolderId;
        updateRequest.RemoveParents = sourceFolderId;
        updateRequest.Fields = "id, parents";

        await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
