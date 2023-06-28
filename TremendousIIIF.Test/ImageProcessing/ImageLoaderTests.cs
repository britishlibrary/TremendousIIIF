using LazyCache;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using System.Net;
using TremendousIIIF.Common;
using TremendousIIIF.ImageProcessing;
using TremendousIIIF.Test.Utilities;
using ILogger = Serilog.ILogger;

namespace TremendousIIIF.Test.ImageProcessing
{
    public class ImageLoaderTests
    {
        [Theory]
        [InlineData("ImageProcessing/test_image.tif", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.jp2", ImageFormat.jp2)]
        public async Task GetSourceFormat_Local(string filename, ImageFormat format)
        {
            var mockClient = new Mock<IHttpClientFactory>();
            var mockLoger = new Mock<ILogger>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLoger.Object, mockCache.Object, mockClient.Object);
            var imageUri = Path.GetFullPath(filename);

            var fs = new FileStream(imageUri, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, useAsync: true);
            var result = await loader.GetSourceFormat(fs, CancellationToken.None);

            Assert.Equal(format, result);
        }

        [Fact]
        public async Task GetSourceFormat_Unsupported_Local()
        {
            var mockClient = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = Path.GetFullPath("ImageProcessing/test_image.png");
            var fs = File.OpenRead(imageUri);

            await Assert.ThrowsAsync<IOException>(async () => await loader.GetSourceFormat(fs, CancellationToken.None));
        }

        [Fact]
        public async Task GetMetadata_Unsupported_Uri()
        {
            var mockClient = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = new Uri("gopher://example.com/test_image.png");

            await Assert.ThrowsAsync<IOException>(async () => await loader.GetMetadata(imageUri, 256, CancellationToken.None));
        }

        [Fact]
        public async Task GetMetadata_Http_NotFound()
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(new HttpResponseMessage(HttpStatusCode.NotFound))
                .Verifiable();

            var httpClient = new HttpClient(handler.Object);
            var mockClient = new Mock<IHttpClientFactory>();
            mockClient.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var mockLogger = new Mock<ILogger>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = new Uri("http://example.com/test_image.png");

            await Assert.ThrowsAsync<FileNotFoundException>(async () => await loader.GetMetadata(imageUri, 256, CancellationToken.None));
        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.ProxyAuthenticationRequired)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetSourceFormat_Http_Failures(HttpStatusCode code)
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(new HttpResponseMessage(code))
                .Verifiable();

            var httpClient = new HttpClient(handler.Object);
            var mockClient = new Mock<IHttpClientFactory>();
            mockClient.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var mockLogger = new Mock<ILogger>();

            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = new Uri("http://example.com/test_image.png");

            await Assert.ThrowsAsync<IOException>(async () => await loader.GetMetadata(imageUri, 256, CancellationToken.None));
            handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
           // mockLogger.Verify(v => v.Error(It.IsAny<string>()), Times.Once);
        }


        [Theory]
        [InlineData("ImageProcessing/test_image.jp2", "image/jp2", ImageFormat.jp2)]
        [InlineData("ImageProcessing/test_image.jp2", "image/jpeg2000", ImageFormat.jp2)]
        [InlineData("ImageProcessing/test_image.jp2", "image/jpeg2000-image", ImageFormat.jp2)]
        [InlineData("ImageProcessing/test_image.jp2", "image/x-jpeg2000-image", ImageFormat.jp2)]
        [InlineData("ImageProcessing/test_image.tif", "image/tif", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "image/tiff", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "image/x-tif", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "image/x-tiff", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "application/tif", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "application/x-tif", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "application/tiff", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "application/x-tiff", ImageFormat.tif)]
        public async Task LoadHttp_Correct_ContentType(string filename, string mimetype, ImageFormat format)
        {
            var tiff = Path.GetFullPath(filename);
            using (var fs = File.OpenRead(tiff))
            {
                var handler = new Mock<MockHttpHandler>() { CallBase = true };
                handler
                    .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                    .Returns(() =>
                    {
                        var m = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StreamContent(fs)
                        };
                        m.Content.Headers.Clear();
                        m.Content.Headers.Add("Content-Type", mimetype);
                        return m;
                    })
                    .Verifiable();

                var httpClient = new HttpClient(handler.Object);
                var mockClient = new Mock<IHttpClientFactory>();
                mockClient.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(httpClient);
                var mockLogger = new Mock<ILogger>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
                var imageUri = new Uri("http://example.com/test_image.tif");

                (var result, _) = await loader.LoadHttp(imageUri, CancellationToken.None);

                Assert.Equal(format, result);
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
            }
        }

        [Theory]
        [InlineData("ImageProcessing/test_image.tif", "application/octet-stream", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "text/plain", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.tif", "text/plain;charset=UTF-8", ImageFormat.tif)]
        [InlineData("ImageProcessing/test_image.jp2", "application/octet-stream", ImageFormat.jp2)]
        [InlineData("ImageProcessing/test_image.jp2", "text/plain", ImageFormat.jp2)]
        [InlineData("ImageProcessing/test_image.jp2", "text/plain;charset=UTF-8", ImageFormat.jp2)]
        public async Task GetSourceFormat_Http_Wrong_ContentType_fallback_to_Magic_Bytes(string fileName, string mimetype, ImageFormat format)
        {
            var tiff = Path.GetFullPath(fileName);
            using (var fs = File.OpenRead(tiff))
            {
                var handler = new Mock<MockHttpHandler>() { CallBase = true };
                handler
                    .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                    .Returns(() =>
                    {
                        var m = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StreamContent(fs)
                        };
                        m.Content.Headers.Clear();
                        m.Content.Headers.TryAddWithoutValidation("Content-Type", mimetype);

                        return m;
                    })
                    .Verifiable();

                var httpClient = new HttpClient(handler.Object);
                var mockClient = new Mock<IHttpClientFactory>();
                mockClient.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(httpClient);
                var mockLogger = new Mock<ILogger>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
                var imageUri = new Uri("http://example.com/test_image.tif");

                (var result, _) = await loader.LoadHttp(imageUri, CancellationToken.None);

                Assert.Equal(format, result);
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task GetSourceFormat_Unsupported_Http_With_Correct_MimeType()
        {
            var tiff = Path.GetFullPath(@"ImageProcessing/test_image.png");
            using (var fs = File.OpenRead(tiff))
            {
                var handler = new Mock<MockHttpHandler>() { CallBase = true };
                handler
                    .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                    .Returns(() =>
                    {
                        var m = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StreamContent(fs)
                        };
                        m.Content.Headers.Add("content-type", "image/png");
                        return m;
                    })
                    .Verifiable();

                var httpClient = new HttpClient(handler.Object);
                var mockClient = new Mock<IHttpClientFactory>();
                mockClient.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(httpClient);
                var mockLogger = new Mock<ILogger>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);

                var imageUri = new Uri("http://example.com/test_image.tif");

                await Assert.ThrowsAsync<IOException>(async () => await loader.LoadHttp(imageUri, CancellationToken.None));

                await Task.WhenAll();
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
            }
        }

        [Fact]
        public async Task GetSourceFormat_Unsupported_Http_With_Incorrect_MimeType()
        {
            var tiff = Path.GetFullPath(@"ImageProcessing/test_image.png");
            using (var fs = File.OpenRead(tiff))
            {
                var handler = new Mock<MockHttpHandler>() { CallBase = true };
                handler
                    .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                    .Returns(() =>
                    {
                        var m = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StreamContent(fs)
                        };
                        m.Content.Headers.Add("content-type", "text/plain");
                        return m;
                    })
                    .Verifiable();

                var httpClient = new HttpClient(handler.Object);
                var mockClient = new Mock<IHttpClientFactory>();
                mockClient.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(httpClient);
                var mockLogger = new Mock<ILogger>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
                var imageUri = new Uri("http://example.com/test_image.png");

                await Assert.ThrowsAsync<IOException>(async () => await loader.LoadHttp(imageUri, CancellationToken.None));

                await Task.WhenAll();
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
               
            }
        }
    }
}