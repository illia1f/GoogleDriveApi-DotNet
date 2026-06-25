using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Helpers;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Operations;

/// <inheritdoc cref="IGDriveFolderOperations"/>
internal sealed class GDriveFolderOperations(IGDriveOperationContext context) : IGDriveFolderOperations
{
    private readonly IGDriveOperationContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<string?> FindIdByNameAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _context.RootFolderId;

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var listRequest = service.Files.List();
        listRequest.Q = $"mimeType='{GDriveMimeTypes.Folder}' and name='{DriveQueryHelper.EscapeValue(folderName)}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 1;

        var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        GoogleFile? file = result.Files?.FirstOrDefault();

        return file?.Id;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(string id, string name)>> ListAsync(string parentFolderId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentFolderId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var allFolders = new List<GoogleFile>();
        string? pageToken = null;
        string qSelector = $"mimeType='{GDriveMimeTypes.Folder}' and '{DriveQueryHelper.EscapeValue(parentFolderId)}' in parents and trashed=false";
        const string fields = "nextPageToken, files(id, name)";
        do
        {
            var listRequest = service.Files.List();
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

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DriveItem>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var request = service.Files.List();
        request.Q = $"mimeType = '{GDriveMimeTypes.Folder}'";
        request.Fields = "nextPageToken, files(id, name, mimeType, parents)";
        request.PageSize = 1000; // Set page size to maximum (1000)

        var folders = new List<GoogleFile>();
        do
        {
            var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (result.Files is not null)
            {
                folders.AddRange(result.Files);
            }

            request.PageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(request.PageToken));

        return folders.Select(f => f.ToDriveItem()).ToList();
    }

    /// <inheritdoc/>
    public async Task<string> CreateAsync(string folderName, string? parentFolderId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderName);

        parentFolderId ??= _context.RootFolderId;

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var driveFolder = new GoogleFile()
        {
            Name = folderName,
            MimeType = GDriveMimeTypes.Folder,
            Parents = [parentFolderId]
        };

        var request = service.Files.Create(driveFolder);
        GoogleFile file = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return file.Id;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string folderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(folderId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFolder();

        await service.Files.Delete(folderId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RenameAsync(string folderId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(folderId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFolder();

        var metadata = new GoogleFile { Name = newName };

        var updateRequest = service.Files.Update(metadata, folderId);
        updateRequest.Fields = "id,name";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MoveAsync(string folderId, string sourceFolderId, string destinationFolderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderId);
        ArgumentException.ThrowIfNullOrEmpty(sourceFolderId);
        ArgumentException.ThrowIfNullOrEmpty(destinationFolderId);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        MimeType mimeType = await service.GetMimeTypeAsync(folderId, cancellationToken).ConfigureAwait(false);
        mimeType.RequireFolder();

        var metadata = new GoogleFile();

        var updateRequest = service.Files.Update(metadata, folderId);
        updateRequest.AddParents = destinationFolderId;
        updateRequest.RemoveParents = sourceFolderId;
        updateRequest.Fields = "id, parents";

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
