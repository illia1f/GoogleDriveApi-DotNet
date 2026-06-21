using GoogleDriveApi_DotNet.Abstractions;
using GoogleDriveApi_DotNet.Exceptions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class GDriveFileOperationsTests
    {
        [Fact]
        public async Task Files_IsExposedAfterBuild()
        {
            GoogleDriveApi api = await BuildUnauthorizedAsync();

            api.Files.ShouldNotBeNull();
            api.Files.ShouldBeAssignableTo<IGDriveFileOperations>();
        }

        [Fact]
        public async Task Files_ListAsync_BeforeAuthorization_ThrowsAuthorizationException()
        {
            GoogleDriveApi api = await BuildUnauthorizedAsync();

            await Should.ThrowAsync<AuthorizationException>(() => api.Files.ListAsync());
        }

        [Fact]
        public async Task Files_DeleteAsync_BeforeAuthorization_ThrowsAuthorizationException()
        {
            GoogleDriveApi api = await BuildUnauthorizedAsync();

            await Should.ThrowAsync<AuthorizationException>(() => api.Files.DeleteAsync("file-id"));
        }

        [Fact]
        public async Task Files_ListAsync_WithNonPositivePageSize_ThrowsArgumentOutOfRange()
        {
            GoogleDriveApi api = await BuildUnauthorizedAsync();

            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => api.Files.ListAsync(pageSize: 0));
        }

        [Fact]
        public async Task Files_FindIdByNameAsync_WithEmptyName_ThrowsArgument()
        {
            GoogleDriveApi api = await BuildUnauthorizedAsync();

            await Should.ThrowAsync<ArgumentException>(() => api.Files.FindIdByNameAsync(string.Empty));
        }

        private static Task<GoogleDriveApi> BuildUnauthorizedAsync()
            => GoogleDriveApi.CreateBuilder().BuildAsync(immediateAuthorization: false);
    }
}
