# Token Refresh Internals

How automatic token refresh works under the hood. User-facing guidance ("do I need to refresh
manually?") lives in [guides/token-and-auth.md](../guides/token-and-auth.md).

## Handled by Google.Apis

No manual token refresh is required — the `Google.Apis` library handles it automatically:

1. `UserCredential` implements `IHttpExecuteInterceptor`.
2. When passed as `HttpClientInitializer` to `DriveService`, it intercepts every HTTP request.
3. `InterceptAsync()` automatically refreshes the token if:
   - no access token exists, or
   - the token is within 1 minute of expiration.
4. It also handles `401` responses by refreshing and retrying.

## References

- [Google OAuth 2.0 Documentation](https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth)
- [UserCredential source](https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth/OAuth2/UserCredential.cs)

## Optional manual refresh

`TryRefreshTokenAsync()` is retained as an optional public method for advanced scenarios (e.g.,
proactively refreshing before a batch of operations). It is **not** part of the normal request
path — the interceptor above already covers normal usage.
