using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class GDriveTrashOperationsTests
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
        public void Trash_IsExposedOnClient()
        {
            var api = GoogleDriveApi.Create(_options, Substitute.For<IGoogleDriveAuthProvider>());

            api.Trash.ShouldNotBeNull();
            api.Trash.ShouldBeAssignableTo<IGDriveTrashOperations>();
        }

        [Fact]
        public async Task Trash_TrashAsync_BeforeAuthorization_AuthorizesLazily()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            authProvider.AuthorizeAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("auth attempted"));
            var api = GoogleDriveApi.Create(_options, authProvider);

            // No explicit AuthorizeAsync call: the first operation must authorize on demand.
            await Should.ThrowAsync<InvalidOperationException>(() => api.Trash.TrashAsync("file-id"));
            await authProvider.Received(1).AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Trash_TrashAsync_WithEmptyId_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Trash.TrashAsync(string.Empty));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Trash_RestoreAsync_WithEmptyId_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentException>(() => api.Trash.RestoreAsync(string.Empty));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Trash_ListAsync_WithNonPositivePageSize_ThrowsBeforeAuthorizing()
        {
            var authProvider = Substitute.For<IGoogleDriveAuthProvider>();
            var api = GoogleDriveApi.Create(_options, authProvider);

            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => api.Trash.ListAsync(pageSize: 0));
            await authProvider.DidNotReceive().AuthorizeAsync(Arg.Any<CancellationToken>());
        }
    }
}
