using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit;

public class GoogleDriveApiAuthTests
{
    #region Authorization Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authorize_WithValidProvider_ShouldAuthorizeSuccessfully(bool useAsync)
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);

        if (useAsync)
        {
            await api.AuthorizeAsync();
        }
        else
        {
            api.Authorize();
        }

        api.IsAuthorized.ShouldBeTrue();
        await mockAuthProvider.Received(1).AuthorizeAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authorize_WhenAlreadyAuthorized_ShouldThrowAuthorizationException(bool useAsync)
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        await api.AuthorizeAsync();

        if (useAsync)
        {
            await Should.ThrowAsync<AuthorizationException>(async () =>
            {
                await api.AuthorizeAsync();
            });
        }
        else
        {
            Should.Throw<AuthorizationException>(() =>
            {
                api.Authorize();
            });
        }
    }

    [Fact]
    public async Task AuthorizeAsync_WithCancellationToken_ShouldPassTokenToProvider()
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        using var cts = new CancellationTokenSource();

        await api.AuthorizeAsync(cts.Token);

        await mockAuthProvider.Received(1).AuthorizeAsync(cts.Token);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        var mockAuthProvider = Substitute.For<IGoogleDriveAuthProvider>();
        mockAuthProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync<OperationCanceledException>();

        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await api.AuthorizeAsync(cts.Token);
        });
    }

    [Fact]
    public void Authorize_WhenProviderThrowsException_ShouldPropagateException()
    {
        var mockAuthProvider = Substitute.For<IGoogleDriveAuthProvider>();
        mockAuthProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Auth failed"));

        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);

        Should.Throw<InvalidOperationException>(() =>
        {
            api.Authorize();
        });
    }

    #endregion

    #region IsAuthorized Tests

    [Theory]
    [InlineData(false, false)]  // Before authorization -> should be false
    [InlineData(true, true)]    // After authorization -> should be true
    public async Task IsAuthorized_ShouldReflectAuthorizationState(bool shouldAuthorize, bool expectedResult)
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);

        if (shouldAuthorize)
        {
            await api.AuthorizeAsync();
        }

        api.IsAuthorized.ShouldBe(expectedResult);
    }

    #endregion

    #region Token Refresh Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryRefreshToken_WhenNotAuthorized_ShouldReturnFalse(bool useAsync)
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);

        var result = useAsync ? await api.TryRefreshTokenAsync() : api.TryRefreshToken();

        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, false)]  // Token not stale -> should return false
    [InlineData(true, true)]    // Token is stale -> should return true
    public async Task TryRefreshToken_WhenAuthorized_ShouldReturnBasedOnStaleness(bool isStale, bool expectedResult)
    {
        var credential = CreateTestUserCredential(isStale: isStale);
        var mockAuthProvider = CreateMockAuthProvider(credential);
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        await api.AuthorizeAsync();

        api.IsTokenShouldBeRefreshed.ShouldBe(isStale);

        var result = await api.TryRefreshTokenAsync();

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryRefreshToken_WhenTokenIsStale_BothSyncAndAsyncShouldRefresh(bool useAsync)
    {
        var credential = CreateTestUserCredential(isStale: true);
        var mockAuthProvider = CreateMockAuthProvider(credential);
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        await api.AuthorizeAsync();

        api.IsTokenShouldBeRefreshed.ShouldBeTrue();

        var result = useAsync ? await api.TryRefreshTokenAsync() : api.TryRefreshToken();

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryRefreshTokenAsync_WithCancellationToken_ShouldNotThrow()
    {
        var credential = CreateTestUserCredential(isStale: true);
        var mockAuthProvider = CreateMockAuthProvider(credential);
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        await api.AuthorizeAsync();
        using var cts = new CancellationTokenSource();

        var result = await api.TryRefreshTokenAsync(cts.Token);

        result.ShouldBeTrue();
    }

    #endregion

    #region IsTokenShouldBeRefreshed Tests

    [Fact]
    public void IsTokenShouldBeRefreshed_WhenNotAuthorized_ShouldReturnFalse()
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);

        api.IsTokenShouldBeRefreshed.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, false)]  // Token not stale -> should return false
    [InlineData(true, true)]    // Token is stale -> should return true
    public async Task IsTokenShouldBeRefreshed_WhenAuthorized_ShouldMatchStaleness(bool isStale, bool expectedResult)
    {
        var credential = CreateTestUserCredential(isStale: isStale);
        var mockAuthProvider = CreateMockAuthProvider(credential);
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        await api.AuthorizeAsync();

        api.IsTokenShouldBeRefreshed.ShouldBe(expectedResult);
    }

    #endregion

    #region Provider Property Tests

    [Fact]
    public async Task Provider_WhenAuthorized_ShouldReturnDriveService()
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);
        await api.AuthorizeAsync();

        api.Provider.ShouldNotBeNull();
    }

    [Fact]
    public void Provider_WhenNotAuthorized_ShouldThrowAuthorizationException()
    {
        var mockAuthProvider = CreateMockAuthProvider();
        var api = GoogleDriveApi.Create(_defaultOptions, mockAuthProvider);

        Should.Throw<AuthorizationException>(() =>
        {
            _ = api.Provider;
        });
    }

    #endregion

    #region Builder Integration Tests

    [Fact]
    public async Task Builder_WithCustomAuthProvider_ShouldUseProvidedAuthProvider()
    {
        var mockAuthProvider = CreateMockAuthProvider();

        var api = await GoogleDriveApi.CreateBuilder()
            .SetAuthProvider(mockAuthProvider)
            .BuildAsync(immediateAuthorization: true);

        api.IsAuthorized.ShouldBeTrue();
        await mockAuthProvider.Received(1).AuthorizeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Builder_WithCustomAuthProvider_NoImmediateAuth_ShouldNotCallAuthProvider()
    {
        var mockAuthProvider = CreateMockAuthProvider();

        var api = await GoogleDriveApi.CreateBuilder()
            .SetAuthProvider(mockAuthProvider)
            .BuildAsync(immediateAuthorization: false);

        api.IsAuthorized.ShouldBeFalse();
        await mockAuthProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Builder_WithFailingAuthProvider_ShouldPropagateException()
    {
        var mockAuthProvider = Substitute.For<IGoogleDriveAuthProvider>();
        mockAuthProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Auth provider failed"));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await GoogleDriveApi.CreateBuilder()
                .SetAuthProvider(mockAuthProvider)
                .BuildAsync(immediateAuthorization: true);
        });
    }

    #endregion

    #region Initialization

    private readonly GoogleDriveApiOptions _defaultOptions;
    public GoogleDriveApiAuthTests()
    {
        _defaultOptions = new GoogleDriveApiOptions
        {
            CredentialsPath = "test-credentials.json",
            TokenFolderPath = "test-tokens",
            UserId = "test-user",
            ApplicationName = "TestApp",
            RootFolderId = "test-root"
        };
    }

    #endregion

    #region Helper Methods

    private static IGoogleDriveAuthProvider CreateMockAuthProvider(UserCredential? credential = null)
    {
        var mockAuthProvider = Substitute.For<IGoogleDriveAuthProvider>();
        credential ??= CreateTestUserCredential(isStale: false);

        mockAuthProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(credential));

        return mockAuthProvider;
    }

    private static UserCredential CreateTestUserCredential(bool isStale)
    {
        var tokenResponse = new TokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresInSeconds = isStale ? 0 : 3600,
            IssuedUtc = isStale ? DateTime.UtcNow.AddHours(-2) : DateTime.UtcNow
        };

        var refreshedToken = new TokenResponse
        {
            AccessToken = "refreshed-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresInSeconds = 3600,
            IssuedUtc = DateTime.UtcNow
        };

        var flow = Substitute.For<Google.Apis.Auth.OAuth2.Flows.IAuthorizationCodeFlow>();
        flow.RefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(refreshedToken));

        // Create a real UserCredential instance with test data
        return new UserCredential(flow, userId: "test-user", tokenResponse);
    }

    #endregion
}