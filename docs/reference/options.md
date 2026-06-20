# Options Reference

Configuration for a `GoogleDriveApi` instance. Set these through the fluent builder
(`GoogleDriveApi.CreateBuilder()`); they are also exposed on `GoogleDriveApiOptions`.

## Settings

- **`CredentialsPath`** — `SetCredentialsPath(string)`, default `"credentials.json"`
  Path to the OAuth client credentials JSON downloaded from Google Cloud Console.
- **`TokenFolderPath`** — `SetTokenFolderPath(string)`, default `"_metadata"`
  Folder where the cached OAuth token is stored after the first authorization.
- **`UserId`** — `SetUserId(string)`, default `"user"`
  Key under which the token is cached. Change it to keep multiple accounts separate when they
  share one `TokenFolderPath`.
- **`RootFolderId`** — `SetRootFolderId(string)`, default `"root"`
  Default parent folder used when an operation's `parentFolderId` is omitted.
- **`ApplicationName`** — `SetApplicationName(string)`, default `null`
  Application name reported to the Google API (optional).
- **Custom auth** — `SetAuthProvider(IGoogleDriveAuthProvider)`, default file-based provider
  Supply a custom authentication provider (e.g., for testing/mocking).

## Builder lifecycle

```csharp
using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetUserId("user")
    .SetRootFolderId("root")
    .SetApplicationName("My Drive App")
    .BuildAsync(immediateAuthorization: true); // default; authorizes during Build
```

- `BuildAsync(immediateAuthorization: true)` (default) authorizes before returning.
- `BuildAsync(immediateAuthorization: false)` returns an unauthorized instance — call
  `AuthorizeAsync` yourself (useful to control timing or apply a timeout token).
- Both `BuildAsync` and `AuthorizeAsync` accept a `CancellationToken`.

## Reading options at runtime

```csharp
GoogleDriveApiOptions options = gDriveApi.Options;
string root = gDriveApi.RootFolderId;
```

## Defaults as constants

The defaults are exposed as constants on `GoogleDriveApiOptions`:
`DefaultCredentialsPath`, `DefaultTokenFolderPath`, `DefaultUserId`, `DefaultRootFolderId`.

> For Cloud Console setup that produces `credentials.json`, see
> [Getting Started](../getting-started.md). For token behavior, see
> [Token and auth](../guides/token-and-auth.md).
