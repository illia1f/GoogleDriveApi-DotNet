namespace GoogleDriveApi_DotNet;

/// <summary>
/// Builder for creating and configuring <see cref="GoogleDriveApi"/> instances.
/// </summary>
public interface IGoogleDriveApiBuilder
{
    /// <summary>
    /// Sets the path to the credentials JSON file. Default value is "credentials.json".
    /// **Note**: Place the downloaded JSON file in your project directory.
    /// <para>Documentation: https://developers.google.com/identity/protocols/oauth2</para>
    /// </summary>
    /// <param name="path">The path to the credentials JSON file.</param>
    /// <returns>The builder instance.</returns>
    IGoogleDriveApiBuilder SetCredentialsPath(string path);

    /// <summary>
    /// Sets the path to the token JSON file folder where it will store the token in a file. Default value is "_metadata".
    /// <para>Documentation: https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth</para>
    /// </summary>
    /// <param name="folderPath">The path to the token JSON file.</param>
    /// <returns>The builder instance.</returns>
    IGoogleDriveApiBuilder SetTokenFolderPath(string folderPath);

    /// <summary>
    /// Sets the user identifier used to store and retrieve tokens in the token data store. Default value is "user".
    /// Change it to avoid token collisions when authorizing multiple accounts while sharing the same token folder.
    /// </summary>
    /// <param name="userId">User identifier used as token cache key.</param>
    /// <returns>The builder instance.</returns>
    IGoogleDriveApiBuilder SetUserId(string userId);

    /// <summary>
    /// Sets the name of the application. Default value is null.
    /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Services.BaseClientService.Initializer#Google_Apis_Services_BaseClientService_Initializer_ApplicationName</para>
    /// </summary>
    /// <param name="name">The name of the application.</param>
    /// <returns>The builder instance.</returns>
    IGoogleDriveApiBuilder SetApplicationName(string name);

    /// <summary>
    /// Sets the root folder ID to use as default parent folder. Default value is "root".
    /// </summary>
    /// <param name="rootFolderId">The root folder ID to use as default parent folder.</param>
    /// <returns>The builder instance.</returns>
    IGoogleDriveApiBuilder SetRootFolderId(string rootFolderId);

    /// <summary>
    /// Sets a custom authentication provider. If not set, a default <see cref="GoogleDriveAuthProvider"/> will be used.
    /// This allows for custom authentication implementations or mocking during tests.
    /// </summary>
    /// <param name="authProvider">The authentication provider to use.</param>
    /// <returns>The builder instance.</returns>
    IGoogleDriveApiBuilder SetAuthProvider(Abstractions.IGoogleDriveAuthProvider authProvider);

    /// <summary>
    /// Builds the GoogleDriveApi instance asynchronously and attempts to authorize if <paramref name="immediateAuthorization"/> is true.
    /// Use <paramref name="cancellationToken"/> to cancel the operation or set a timeout (e.g., <c>new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token</c>).
    /// <para>Documentation: https://cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker?hl=en#Google_Apis_Auth_OAuth2_GoogleWebAuthorizationBroker_AuthorizeAsync_Google_Apis_Auth_OAuth2_ClientSecrets_System_Collections_Generic_IEnumerable_System_String__System_String_System_Threading_CancellationToken_Google_Apis_Util_Store_IDataStore_Google_Apis_Auth_OAuth2_ICodeReceiver_</para>
    /// </summary>
    /// <param name="immediateAuthorization">Whether to authorize immediately after building.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation or set a timeout.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the authorized GoogleDriveApi instance.</returns>
    Task<GoogleDriveApi> BuildAsync(bool immediateAuthorization = true, CancellationToken cancellationToken = default);
}