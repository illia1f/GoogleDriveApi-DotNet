using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class DriveFilesTests
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
        public void Files_IsExposedOnClient()
        {
            var api = GoogleDriveApi.Create(_options, Substitute.For<IGoogleDriveAuthProvider>());

            api.Files.ShouldNotBeNull();
            api.Files.ShouldBeAssignableTo<IDriveFiles>();
        }

        [Fact]
        public async Task Files_DeleteAsync_BeforeAuthorization_AuthorizesLazily()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            authProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("auth attempted"));
            var api = GoogleDriveApi.Create(_options, authProvider);

            // No explicit AuthorizeAsync call: the first operation must authorize on demand.
            await Should.ThrowAsync<InvalidOperationException>(() => api.Files.DeleteAsync("file-id"));
            await authProvider.Received(1).AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Files_ListAsync_WithNonPositivePageSize_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => api.Files.ListAsync(pageSize: 0));

            // Bad arguments must never trigger a login.
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Files_FindIdByNameAsync_WithEmptyName_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Files.FindIdByNameAsync(string.Empty));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null, "new-name")]
        [InlineData("", "new-name")]
        [InlineData("file-id", null)]
        [InlineData("file-id", "")]
        public async Task Files_RenameAsync_WithMissingArgument_ThrowsBeforeAuthorizing(string? fileId, string? newName)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Files.RenameAsync(fileId!, newName!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null, "source", "destination")]
        [InlineData("", "source", "destination")]
        [InlineData("file-id", null, "destination")]
        [InlineData("file-id", "", "destination")]
        [InlineData("file-id", "source", null)]
        [InlineData("file-id", "source", "")]
        public async Task Files_MoveAsync_WithMissingArgument_ThrowsBeforeAuthorizing(string? fileId, string? sourceFolderId, string? destinationFolderId)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Files.MoveAsync(fileId!, sourceFolderId!, destinationFolderId!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null, "destination")]
        [InlineData("", "destination")]
        [InlineData("file-id", null)]
        [InlineData("file-id", "")]
        public async Task Files_CopyAsync_WithMissingArgument_ThrowsBeforeAuthorizing(string? fileId, string? destinationFolderId)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Files.CopyAsync(fileId!, destinationFolderId!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }
    }
}
