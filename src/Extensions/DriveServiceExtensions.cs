using Google.Apis.Drive.v3;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Extensions;

internal static class DriveServiceExtensions
{
    /// <summary>
    /// Fetches only the MIME type of a single Drive item. Used to validate an item's kind without
    /// over-fetching the rest of its metadata.
    /// </summary>
    /// <param name="service">The authorized Drive service.</param>
    /// <param name="itemId">The ID of the item to inspect.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public static async Task<MimeType> GetMimeTypeAsync(this DriveService service, string itemId, CancellationToken cancellationToken)
    {
        var request = service.Files.Get(itemId);
        request.Fields = "mimeType";

        GoogleFile file = await request
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return MimeType.Create(file.MimeType);
    }
}
