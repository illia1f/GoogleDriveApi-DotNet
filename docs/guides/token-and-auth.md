# Token and Auth

What you need to know about authorization and tokens as a user of the library. For first-time
setup (Cloud Console + credentials), see [Getting Started](../getting-started.md).

## Do I need to refresh tokens manually?

**No.** Token refresh is automatic. The underlying `Google.Apis` library refreshes the access
token before requests and after `401` responses, so you do not have to manage token lifetime.

The first time you authorize, a token is cached under the folder you pass to
`SetTokenFolderPath` (default `_metadata`). Later runs reuse it without prompting.

For the mechanism behind this, see
[token refresh internals](../contributing/token-refresh-internals.md).

## Optional manual refresh

`TryRefreshTokenAsync()` is available for advanced scenarios where you want to refresh
proactively (for example, before a long batch of operations). It is **not required** for
normal usage and returns `false` when no refresh was needed.

```csharp
bool refreshed = await gDriveApi.TryRefreshTokenAsync(); // Add: cancellationToken
```

## Checking authorization state

```csharp
if (!gDriveApi.IsAuthorized)
    await gDriveApi.AuthorizeAsync();

bool stale = gDriveApi.IsTokenShouldBeRefreshed; // true if the cached token is near expiry
```

## References

- [Google OAuth 2.0 Documentation](https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth)
