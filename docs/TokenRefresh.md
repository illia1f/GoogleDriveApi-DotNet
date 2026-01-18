# Token Refresh

## Handled by Google.Apis

**No manual token refresh is required.** The Google.Apis library handles this automatically:

1. `UserCredential` implements `IHttpExecuteInterceptor`
2. When passed as `HttpClientInitializer` to `DriveService`, it intercepts every HTTP request
3. `InterceptAsync()` automatically refreshes the token if:
   - No access token exists
   - Token is within 1 minute of expiration
4. Also handles 401 responses by refreshing and retrying

**References:**
- [Google OAuth 2.0 Documentation](https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth)
- [UserCredential Source](https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth/OAuth2/UserCredential.cs)

## Optional Manual Refresh

`TryRefreshTokenAsync()` is retained as an optional public method for advanced scenarios where users want to proactively refresh tokens (e.g., before a batch of operations). It is **not required** for normal usage.
