using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class DriveFoldersTests
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
        public void Folders_IsExposedOnClient()
        {
            var api = GoogleDriveApi.Create(_options, Substitute.For<IGoogleDriveAuthProvider>());

            api.Folders.ShouldNotBeNull();
            api.Folders.ShouldBeAssignableTo<IDriveFolders>();
        }

        [Fact]
        public async Task Folders_DeleteAsync_BeforeAuthorization_AuthorizesLazily()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            authProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("auth attempted"));
            var api = GoogleDriveApi.Create(_options, authProvider);

            // No explicit AuthorizeAsync call: the first operation must authorize on demand.
            await Should.ThrowAsync<InvalidOperationException>(() => api.Folders.DeleteAsync("folder-id"));
            await authProvider.Received(1).AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Folders_FindIdByNameAsync_WithEmptyName_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Folders.FindIdByNameAsync(string.Empty));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Folders_CreateAsync_WithEmptyName_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Folders.CreateAsync(string.Empty));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Folders_DeleteAsync_WithMissingId_ThrowsBeforeAuthorizing(string? folderId)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Folders.DeleteAsync(folderId!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Folders_ListAsync_WithMissingParent_ThrowsBeforeAuthorizing(string? parentFolderId)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Folders.ListAsync(parentFolderId!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Folders_ListAsync_WithNonPositivePageSize_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => api.Folders.ListAsync("parent-id", pageSize: 0));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null, "new-name")]
        [InlineData("", "new-name")]
        [InlineData("folder-id", null)]
        [InlineData("folder-id", "")]
        public async Task Folders_RenameAsync_WithMissingArgument_ThrowsBeforeAuthorizing(string? folderId, string? newName)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Folders.RenameAsync(folderId!, newName!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(null, "source", "destination")]
        [InlineData("", "source", "destination")]
        [InlineData("folder-id", null, "destination")]
        [InlineData("folder-id", "", "destination")]
        [InlineData("folder-id", "source", null)]
        [InlineData("folder-id", "source", "")]
        public async Task Folders_MoveAsync_WithMissingArgument_ThrowsBeforeAuthorizing(string? folderId, string? sourceFolderId, string? destinationFolderId)
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Folders.MoveAsync(folderId!, sourceFolderId!, destinationFolderId!));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }
    }
}
