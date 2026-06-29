using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class DriveTransfersTests
    {
        private readonly GoogleDriveApiOptions _options = new()
        {
            CredentialsPath = "test-credentials.json",
            TokenFolderPath = "test-tokens",
            UserId = "test-user",
            ApplicationName = "TestApp",
            RootFolderId = "test-root"
        };

        [Fact]
        public void Transfers_IsExposedOnClient()
        {
            var api = GoogleDriveApi.Create(_options, Substitute.For<IGoogleDriveAuthProvider>());

            api.Transfers.ShouldNotBeNull();
            api.Transfers.ShouldBeAssignableTo<IDriveTransfers>();
        }

        [Fact]
        public async Task Transfers_UpdateContentAsync_BeforeAuthorization_AuthorizesLazily()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            authProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("auth attempted"));
            var api = GoogleDriveApi.Create(_options, authProvider);

            using var content = new MemoryStream([1, 2, 3]);

            // No explicit AuthorizeAsync call: the first operation must authorize on demand.
            await Should.ThrowAsync<InvalidOperationException>(
                () => api.Transfers.UpdateContentAsync("file-id", content, "application/octet-stream"));
            await authProvider.Received(1).AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Transfers_UpdateContentAsync_WithEmptyId_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            using var content = new MemoryStream([1, 2, 3]);

            await Should.ThrowAsync<ArgumentException>(
                () => api.Transfers.UpdateContentAsync(string.Empty, content, "application/octet-stream"));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Transfers_DownloadAsync_WithEmptyId_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Transfers.DownloadAsync(string.Empty));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Transfers_UploadFromPath_WithEmptyPath_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Transfers.UploadAsync(string.Empty, "text/plain"));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Transfers_UploadFromPath_WithMissingFile_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            string missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.txt");

            // A missing file is a usage error: it must surface before any login.
            await Should.ThrowAsync<FileNotFoundException>(() => api.Transfers.UploadAsync(missingPath, "text/plain"));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Transfers_UploadFromStream_WithNullStream_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentNullException>(
                () => api.Transfers.UploadAsync((Stream)null!, "name.txt", "text/plain"));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }
    }
}
