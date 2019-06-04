using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Moq;
using System.Net.Http;
using TremendousIIIF.Common;
using System.Net;
using Microsoft.Extensions.Logging;
using LazyCache;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging.Internal;
using TremendousIIIF.ImageProcessing;

namespace TremendousIIIF.Test.ImageProcessing
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    [TestCategory("Image Loading")]
    public class ImageLoaderTests
    {
        [TestMethod]
        [DataRow("ImageProcessing/test_image.tif", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.jp2", ImageFormat.jp2)]
        public async Task GetSourceFormat_Local(string filename, ImageFormat format)
        {
            var mockClient = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = Path.GetFullPath(filename);

            var fs = new FileStream(imageUri, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, useAsync: true);

            var result = await loader.GetSourceFormat(fs, CancellationToken.None);

            Assert.AreEqual(format, result);

        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public async Task GetSourceFormat_Unsupported_Local()
        {
            var mockClient = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = Path.GetFullPath("ImageProcessing/test_image.png");
            var fs = File.OpenRead(imageUri);

            try
            {
                var result = await loader.GetSourceFormat(fs, CancellationToken.None);

                Assert.AreEqual(ImageFormat.jp2, result);
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public async Task GetMetadata_Unsupported_Uri()
        {
            var mockClient = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = new Uri("gopher://example.com/test_image.png");

            try
            {
                var result = await loader.GetMetadata(imageUri, 256, CancellationToken.None);

                Assert.AreEqual(ImageFormat.jp2, result);
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
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
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = new Uri("http://example.com/test_image.png");

            try
            {
                var result = await loader.GetMetadata(imageUri, 256, CancellationToken.None);

                Assert.AreEqual(ImageFormat.jp2, result);

            }
            catch (AggregateException e)
            {
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        [DataRow(HttpStatusCode.GatewayTimeout)]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.ProxyAuthenticationRequired)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.InternalServerError)]
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
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            //logger.Setup(l => l.Log(LogLevel.Error, It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<HttpStatusCode>(), It.IsAny<string>())).Verifiable();


            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
            var imageUri = new Uri("http://example.com/test_image.png");

            try
            {
                var result = await loader.GetMetadata(imageUri, 256, CancellationToken.None);
            }
            catch (AggregateException e)
            {
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
                mockLogger.Verify(v => v.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
                throw e.InnerException;
            }
        }

        [TestMethod]
        [DataRow("ImageProcessing/test_image.jp2", "image/jp2", ImageFormat.jp2)]
        [DataRow("ImageProcessing/test_image.jp2", "image/jpeg2000", ImageFormat.jp2)]
        [DataRow("ImageProcessing/test_image.jp2", "image/jpeg2000-image", ImageFormat.jp2)]
        [DataRow("ImageProcessing/test_image.jp2", "image/x-jpeg2000-image", ImageFormat.jp2)]
        [DataRow("ImageProcessing/test_image.tif", "image/tif", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "image/tiff", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "image/x-tif", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "image/x-tiff", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "application/tif", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "application/x-tif", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "application/tiff", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "application/x-tiff", ImageFormat.tif)]
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
                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
                var imageUri = new Uri("http://example.com/test_image.tif");

                (var result, _) = await loader.LoadHttp(imageUri, CancellationToken.None);

                Assert.AreEqual(format, result);
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
            }
        }

        [TestMethod]
        [DataRow("ImageProcessing/test_image.tif", "application/octet-stream", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "text/plain", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.tif", "text/plain;charset=UTF-8", ImageFormat.tif)]
        [DataRow("ImageProcessing/test_image.jp2", "application/octet-stream", ImageFormat.jp2)]
        [DataRow("ImageProcessing/test_image.jp2", "text/plain", ImageFormat.jp2)]
        [DataRow("ImageProcessing/test_image.jp2", "text/plain;charset=UTF-8", ImageFormat.jp2)]
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
                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
                var imageUri = new Uri("http://example.com/test_image.tif");

                (var result, _) = await loader.LoadHttp(imageUri, CancellationToken.None);

                Assert.AreEqual(format, result);
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Exactly(1));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
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
                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);

                var imageUri = new Uri("http://example.com/test_image.tif");

                try
                {
                    (var result, _) = await loader.LoadHttp(imageUri, CancellationToken.None);

                    Assert.AreEqual(ImageFormat.jp2, result);
                    handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
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
                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object, mockClient.Object);
                var imageUri = new Uri("http://example.com/test_image.png");

                try
                {
                    (var result, _) = await loader.LoadHttp(imageUri, CancellationToken.None);

                    Assert.AreEqual(ImageFormat.jp2, result);
                    handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Exactly(2));
                }
                catch (AggregateException e)
                {
                    handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Exactly(2));
                    throw e.InnerException;
                }
            }
        }
    }
}
