using Google.Apis.Auth.OAuth2;

namespace GoogleDriveApi_DotNet.Abstractions;

/// <summary>
/// Defines the contract for authentication providers in Google Drive API.
/// </summary>
public interface IGoogleDriveAuthProvider
{
    /// <summary>
    /// Authorizes the user and returns a UserCredential that can be used to access Google Drive.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the UserCredential.</returns>
    Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken = default);
}

