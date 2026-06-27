using Google.Apis.Drive.v3;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Extensions;

internal static class DriveServiceExtensions
{
    /// <summary>
    /// Fetches only the MIME type of a single Drive item. Used to validate an item's kind without
    /// over-fetching the rest of its metadata.
    /// </summary>
    public static async Task<MimeType> GetMimeTypeAsync(this DriveService service, string itemId, CancellationToken cancellationToken)
    {
        var request = service.Files.Get(itemId);
        request.Fields = "mimeType";

        GoogleFile file = await request
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return MimeType.Create(file.MimeType);
    }

    /// <summary>
    /// Runs a <c>Files.List</c> query to completion, following <c>nextPageToken</c> across every page,
    /// and returns the accumulated raw <see cref="GoogleFile"/> items. The single pagination loop shared
    /// by the file, folder, and trash listings.
    /// </summary>
    public static async Task<IReadOnlyList<GoogleFile>> ListAsync(
        this DriveService service,
        string query,
        string fields,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var files = new List<GoogleFile>();
        string? pageToken = null;
        do
        {
            var request = service.Files.List();
            request.Q = query;
            request.Fields = fields;
            request.PageSize = pageSize;
            request.PageToken = pageToken;

            var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (result.Files is not null)
            {
                files.AddRange(result.Files);
            }

            pageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return files;
    }
}
