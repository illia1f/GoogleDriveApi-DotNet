namespace GoogleDriveApi_DotNet;

/// <inheritdoc cref="IGoogleDriveApiBuilder"/>
internal class GoogleDriveApiBuilder : IGoogleDriveApiBuilder
{
    private string _credentialsPath = "credentials.json";
    private string _tokenFolderPath = "_metadata";
    private string? _applicationName = null;

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

    public IGoogleDriveApiBuilder SetApplicationName(string name)
    {
        _applicationName = name;
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
        var options = new Models.GoogleDriveApiOptions
        {
            CredentialsPath = _credentialsPath,
            TokenFolderPath = _tokenFolderPath,
            ApplicationName = _applicationName
        };

        var gDriveApi = GoogleDriveApi.Create(options);

        if (immediateAuthorization)
        {
            await gDriveApi.Internal_AuthorizeAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return gDriveApi;
    }
}