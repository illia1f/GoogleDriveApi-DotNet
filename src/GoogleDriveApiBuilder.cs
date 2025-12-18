using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet;

/// <inheritdoc cref="IGoogleDriveApiBuilder"/>
internal class GoogleDriveApiBuilder : IGoogleDriveApiBuilder
{
    private string _rootFolderId = GoogleDriveApiOptions.DefaultRootFolderId;
    private string _credentialsPath = GoogleDriveApiOptions.DefaultCredentialsPath;
    private string _tokenFolderPath = GoogleDriveApiOptions.DefaultTokenFolderPath;
    private string _userId = GoogleDriveApiOptions.DefaultUserId;
    private string? _applicationName = null;
    private IGoogleDriveAuthProvider? _authProvider = null; 

    public IGoogleDriveApiBuilder SetCredentialsPath(string path)
    {
        _credentialsPath = path;
        return this;
    }

    public IGoogleDriveApiBuilder SetTokenFolderPath(string folderPath)
    {
        _tokenFolderPath = folderPath;
        return this;
    }

    public IGoogleDriveApiBuilder SetUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public IGoogleDriveApiBuilder SetApplicationName(string name)
    {
        _applicationName = name;
        return this;
    }

    public IGoogleDriveApiBuilder SetRootFolderId(string rootFolderId)
    {
        _rootFolderId = rootFolderId;
        return this;
    }

    public IGoogleDriveApiBuilder SetAuthProvider(IGoogleDriveAuthProvider authProvider)
    {
        _authProvider = authProvider;
        return this;
    }

    public GoogleDriveApi Build(bool immediateAuthorization = true, CancellationToken cancellationToken = default)
    {
        return Internal_BuildAsync(immediateAuthorization, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<GoogleDriveApi> BuildAsync(bool immediateAuthorization = true, CancellationToken cancellationToken = default)
    {
        return await Internal_BuildAsync(immediateAuthorization, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<GoogleDriveApi> Internal_BuildAsync(bool immediateAuthorization, CancellationToken cancellationToken)
    {
        // If no custom auth provider is set, create the default one
        var authProvider = _authProvider ?? new GoogleDriveAuthProvider(
            _credentialsPath,
            _tokenFolderPath,
            _userId);

        var options = new GoogleDriveApiOptions
        {
            CredentialsPath = _credentialsPath,
            TokenFolderPath = _tokenFolderPath,
            UserId = _userId,
            ApplicationName = _applicationName,
            RootFolderId = _rootFolderId
        };

        var gDriveApi = GoogleDriveApi.Create(options, authProvider);

        if (immediateAuthorization)
        {
            await gDriveApi.Internal_AuthorizeAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return gDriveApi;
    }
}