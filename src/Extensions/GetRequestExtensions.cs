using System.Net;
using Google;
using Google.Apis.Drive.v3;

namespace GoogleDriveApi_DotNet.Extensions;

internal static class GetRequestExtensions
{
    /// <summary>
    /// Wraps a <c>Files.Get</c> request so that executing it returns <see langword="null"/> when the
    /// requested item does not exist, turning the Drive API's <c>404</c> into a plain absent result
    /// rather than an exception the caller must catch.
    /// </summary>
    /// <param name="request">The <c>Files.Get</c> request to wrap.</param>
    public static NotFoundTolerantGetRequest WithDefaultOnNotFound(this FilesResource.GetRequest request)
        => new(request);
}

/// <summary>
/// A <c>Files.Get</c> request whose <see cref="ExecuteAsync"/> yields <see langword="null"/> on a
/// <c>404</c> Not Found instead of throwing. Every other failure, including <c>403</c> Forbidden,
/// still propagates, since those signal a problem the caller cannot read as a plain absence.
/// </summary>
internal readonly struct NotFoundTolerantGetRequest(FilesResource.GetRequest request)
{
    /// <summary>
    /// Executes the wrapped request, returning the item, or <see langword="null"/> when no item with
    /// that ID exists.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GoogleFile?> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
