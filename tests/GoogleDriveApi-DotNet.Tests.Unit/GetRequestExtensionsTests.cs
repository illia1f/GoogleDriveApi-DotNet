using System.Net;
using System.Text;
using Google;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using GoogleDriveApi_DotNet.Extensions;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class GetRequestExtensionsTests
    {
        [Fact]
        public async Task WithDefaultOnNotFound_OnNotFound_ReturnsNull()
        {
            var service = CreateService(HttpStatusCode.NotFound, """{"error":{"code":404,"message":"File not found"}}""");
            var request = service.Files.Get("missing-id");

            var result = await request.WithDefaultOnNotFound().ExecuteAsync(CancellationToken.None);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task WithDefaultOnNotFound_OnSuccess_ReturnsResponse()
        {
            var service = CreateService(HttpStatusCode.OK, """{"id":"file-1","name":"doc.txt"}""");
            var request = service.Files.Get("file-1");

            var result = await request.WithDefaultOnNotFound().ExecuteAsync(CancellationToken.None);

            result.ShouldNotBeNull();
            result!.Id.ShouldBe("file-1");
        }

        [Fact]
        public async Task WithDefaultOnNotFound_OnForbidden_Propagates()
        {
            var service = CreateService(HttpStatusCode.Forbidden, """{"error":{"code":403,"message":"forbidden"}}""");
            var request = service.Files.Get("file-1");

            var ex = await Should.ThrowAsync<GoogleApiException>(
                () => request.WithDefaultOnNotFound().ExecuteAsync(CancellationToken.None));
            ex.HttpStatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        private static DriveService CreateService(HttpStatusCode status, string body)
        {
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientFactory = new StubHttpClientFactory(new StubMessageHandler(status, body)),
                ApplicationName = "Test"
            });
        }

        /// <summary>
        /// Forces every request issued by the <see cref="DriveService"/> through a canned
        /// response handler, so the Drive HTTP layer can be exercised without a network call.
        /// </summary>
        private sealed class StubHttpClientFactory(HttpMessageHandler handler) : HttpClientFactory
        {
            protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => handler;
        }

        /// <summary>
        /// Returns a fixed status code and body for any request, standing in for the Drive API.
        /// </summary>
        private sealed class StubMessageHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
        }
    }
}
