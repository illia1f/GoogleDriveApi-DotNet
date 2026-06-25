using GoogleDriveApi_DotNet.Abstractions;

namespace GoogleDriveApi_DotNet.Operations;

/// <inheritdoc cref="IGDriveTrashOperations"/>
internal sealed class GDriveTrashOperations(IGDriveOperationContext context) : IGDriveTrashOperations
{
    private readonly IGDriveOperationContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task TrashAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var metadata = new GoogleFile
        {
            Trashed = true
        };

        var updateRequest = service.Files.Update(metadata, fileId);
        updateRequest.Fields = "id, trashed";

        await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var metadata = new GoogleFile
        {
            Trashed = false
        };

        var updateRequest = service.Files.Update(metadata, fileId);
        updateRequest.Fields = "id, trashed";

        await updateRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task EmptyAsync(CancellationToken cancellationToken = default)
    {
        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        await service.Files.EmptyTrash()
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoogleFile>> ListAsync(int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        var trashed = new List<GoogleFile>();
        string? pageToken = null;
        do
        {
            var request = service.Files.List();
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
}
