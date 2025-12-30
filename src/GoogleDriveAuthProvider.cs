using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using GoogleDriveApi_DotNet.Abstractions;

namespace GoogleDriveApi_DotNet;

/// <summary>
/// Default implementation of <see cref="IGoogleDriveAuthProvider"/> that uses OAuth2 with file-based credentials.
/// </summary>
public class GoogleDriveAuthProvider(
    string credentialsPath,
    string tokenFolderPath,
    string userId) : IGoogleDriveAuthProvider
{
    private readonly string _credentialsPath = credentialsPath;
    private readonly string _tokenFolderPath = tokenFolderPath;
    private readonly string _userId = userId;

    /// <inheritdoc/>
    public async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);
        var gcSecrets = await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        var dataStore = new FileDataStore(_tokenFolderPath, fullPath: true);

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets: gcSecrets.Secrets,
            scopes: [DriveService.Scope.Drive],
            user: _userId,
            cancellationToken,
            dataStore).ConfigureAwait(false);
    }
}