using GoogleDriveApi_DotNet.Types;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class GoogleDriveApiBuilderTests
    {
        [Fact]
        public void SetMethods_SupportFluentInterface()
        {
            IGoogleDriveApiBuilder result = _builder
                .SetCredentialsPath("test-credentials.json")
                .SetTokenFolderPath("test-tokens")
                .SetUserId("test-user")
                .SetApplicationName("TestApp")
                .SetRootFolderId("test-root");

            result.ShouldNotBeNull();
            result.ShouldBeSameAs(_builder);
        }

        [Fact]
        public async Task BuildAsync_WithDefaultValues_CreatesApiWithDefaultRootFolderId()
        {
            GoogleDriveApi api = await _builder.BuildAsync(immediateAuthorization: false);

            api.ShouldNotBeNull();
            api.RootFolderId.ShouldBe("root");
        }

        [Fact]
        public async Task BuildAsync_WithCustomValues_ConfiguresApiCorrectly()
        {
            GoogleDriveApiOptions options = new()
            {
                CredentialsPath = "test-credentials.json",
                TokenFolderPath = "test-tokens",
                UserId = "test-user",
                ApplicationName = "TestApplication",
                RootFolderId = "test-root-123"
            };

            GoogleDriveApi api = await _builder
                .SetCredentialsPath(options.CredentialsPath)
                .SetTokenFolderPath(options.TokenFolderPath)
                .SetUserId(options.UserId)
                .SetApplicationName(options.ApplicationName)
                .SetRootFolderId(options.RootFolderId)
                .BuildAsync(immediateAuthorization: false);

            api.ShouldNotBeNull();
            api.Options.ShouldBe(options);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BuildAsync_WithCancellationToken_HandlesCorrectly(bool isCancelled)
        {
            using var cts = new CancellationTokenSource();
            if (isCancelled)
            {
                await cts.CancelAsync();
            }

            // Act - Since immediateAuthorization is false, cancellation shouldn't affect the build
            GoogleDriveApi api = await _builder.BuildAsync(immediateAuthorization: false, cts.Token);

            api.ShouldNotBeNull();
        }

        [Fact]
        public async Task BuildAsync_CalledMultipleTimes_CreatesDistinctInstancesWithSameConfiguration()
        {
            _builder.SetRootFolderId("test-folder");

            GoogleDriveApi api1 = await _builder.BuildAsync(immediateAuthorization: false);
            GoogleDriveApi api2 = await _builder.BuildAsync(immediateAuthorization: false);

            api1.ShouldNotBeNull();
            api2.ShouldNotBeNull();
            api1.ShouldNotBeSameAs(api2);
            api1.Options.ShouldBe(api2.Options);
        }

        [Fact]
        public async Task SetMethods_AcceptNullAndEmptyStrings()
        {
            // Act & Assert - All set methods should accept null or empty without throwing
            _builder
                .SetApplicationName(null!)
                .SetApplicationName(string.Empty)
                .SetCredentialsPath(string.Empty)
                .SetTokenFolderPath(string.Empty)
                .SetUserId(null!)
                .SetUserId(string.Empty)
                .SetRootFolderId(string.Empty)
                .ShouldNotBeNull();

            var api = await _builder.BuildAsync(immediateAuthorization: false);
            api.ShouldNotBeNull();
            api.RootFolderId.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task BuildAsync_WithDefaultImmediateAuthorization_AttemptsAuthorization()
        {
            // This will attempt authorization, which will fail without credentials
            await Should.ThrowAsync<FileNotFoundException>(async () =>
            {
                await _builder.BuildAsync(); // Uses default immediateAuthorization = true
            });
        }

        #region Initialization
        private readonly IGoogleDriveApiBuilder _builder;
        public GoogleDriveApiBuilderTests()
        {
            _builder = GoogleDriveApi.CreateBuilder();
        }
        #endregion
    }
}