using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Types;

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

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
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

        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task EmptyAsync(CancellationToken cancellationToken = default)
    {
        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        await service.Files
            .EmptyTrash()
            .ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DriveItem>> ListAsync(int pageSize = 50, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GoogleFile> trashed = await ListAsync(DriveFields.Default, pageSize, cancellationToken).ConfigureAwait(false);
        return trashed.Select(f => f.ToDriveItem()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoogleFile>> ListAsync(DriveFields fields, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var service = await _context.GetServiceAsync(cancellationToken).ConfigureAwait(false);

        return await service.ListAsync("trashed = true", fields.ToListMask(), pageSize, cancellationToken).ConfigureAwait(false);
    }
}
