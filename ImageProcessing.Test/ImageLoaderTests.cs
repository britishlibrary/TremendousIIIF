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

namespace ImageProcessing.Test
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    [TestCategory("Image Loading")]
    public class ImageLoaderTests
    {
        [TestMethod]
        [DataRow("test_image.tif", ImageFormat.tif)]
        [DataRow("test_image.jp2", ImageFormat.jp2)]
        public async Task GetSourceFormat_Local(string filename, ImageFormat format)
        {
            var mockClient = new Mock<HttpClient>();
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
            var imageUri = new Uri(Path.GetFullPath(filename));

            var result = await loader.GetSourceFormat(mockClient.Object, imageUri, CancellationToken.None);

            Assert.AreEqual(format, result);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void GetSourceFormat_Unsupported_Local()
        {
            var mockClient = new Mock<HttpClient>();
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
            var imageUri = new Uri(Path.GetFullPath("test_image.png"));

            try
            {
                var result = loader.GetSourceFormat(mockClient.Object, imageUri, CancellationToken.None).Result;

                Assert.AreEqual(ImageFormat.jp2, result);
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void GetSourceFormat_Unsupported_Uri()
        {
            var mockClient = new Mock<HttpClient>();
            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
            var imageUri = new Uri("gopher://example.com/test_image.png");

            try
            {
                var result = loader.GetSourceFormat(mockClient.Object, imageUri, CancellationToken.None).Result;

                Assert.AreEqual(ImageFormat.jp2, result);
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void GetSourceFormat_Http_NotFound()
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(new HttpResponseMessage(HttpStatusCode.NotFound))
                .Verifiable();

            var httpClient = new HttpClient(handler.Object);

            var mockLogger = new Mock<ILogger<ImageLoader>>();
            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
            var imageUri = new Uri("http://example.com/test_image.png");

            try
            {
                var result = loader.GetSourceFormat(httpClient, imageUri, CancellationToken.None).Result;

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
        public void GetSourceFormat_Http_Failures(HttpStatusCode code)
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(new HttpResponseMessage(code))
                .Verifiable();

            var httpClient = new HttpClient(handler.Object);

            var mockLogger = new Mock<ILogger<ImageLoader>>();
            //logger.Setup(l => l.Log(LogLevel.Error, It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<HttpStatusCode>(), It.IsAny<string>())).Verifiable();


            var mockCache = new Mock<IAppCache>();
            var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
            var imageUri = new Uri("http://example.com/test_image.png");

            try
            {
                var result = loader.GetSourceFormat(httpClient, imageUri,CancellationToken.None).Result;
            }
            catch (AggregateException e)
            {
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
                mockLogger.Verify(v => v.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
                throw e.InnerException;
            }
        }

        [TestMethod]
        [DataRow("test_image.jp2", "image/jp2", ImageFormat.jp2)]
        [DataRow("test_image.jp2", "image/jpeg2000", ImageFormat.jp2)]
        [DataRow("test_image.jp2", "image/jpeg2000-image", ImageFormat.jp2)]
        [DataRow("test_image.jp2", "image/x-jpeg2000-image", ImageFormat.jp2)]
        [DataRow("test_image.tif", "image/tif", ImageFormat.tif)]
        [DataRow("test_image.tif", "image/tiff", ImageFormat.tif)]
        [DataRow("test_image.tif", "image/x-tif", ImageFormat.tif)]
        [DataRow("test_image.tif", "image/x-tiff", ImageFormat.tif)]
        [DataRow("test_image.tif", "application/tif", ImageFormat.tif)]
        [DataRow("test_image.tif", "application/x-tif", ImageFormat.tif)]
        [DataRow("test_image.tif", "application/tiff", ImageFormat.tif)]
        [DataRow("test_image.tif", "application/x-tiff", ImageFormat.tif)]
        public void GetSourceFormat_Http_Correct_ContentType(string filename, string mimetype, ImageFormat format)
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

                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
                var imageUri = new Uri("http://example.com/test_image.tif");

                var result = loader.GetSourceFormat(httpClient, imageUri, CancellationToken.None).Result;

                Assert.AreEqual(format, result);
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
            }
        }

        [TestMethod]
        [DataRow("test_image.tif", "application/octet-stream", ImageFormat.tif)]
        [DataRow("test_image.tif", "text/plain", ImageFormat.tif)]
        [DataRow("test_image.tif", "text/plain;charset=UTF-8", ImageFormat.tif)]
        [DataRow("test_image.jp2", "application/octet-stream", ImageFormat.jp2)]
        [DataRow("test_image.jp2", "text/plain", ImageFormat.jp2)]
        [DataRow("test_image.jp2", "text/plain;charset=UTF-8", ImageFormat.jp2)]
        public void GetSourceFormat_Http_Wrong_ContentType(string fileName, string mimetype, ImageFormat format)
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
                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
                var imageUri = new Uri("http://example.com/test_image.tif");

                var result = loader.GetSourceFormat(httpClient, imageUri, CancellationToken.None).Result;

                Assert.AreEqual(format, result);
                handler.Verify(v => v.Send(It.IsAny<HttpRequestMessage>()), Times.Exactly(2));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void GetSourceFormat_Unsupported_Http_With_Correct_MimeType()
        {
            var tiff = Path.GetFullPath(@"test_image.png");
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
                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object);

                var imageUri = new Uri("http://example.com/test_image.tif");

                try
                {
                    var result = loader.GetSourceFormat(httpClient, imageUri, CancellationToken.None).Result;

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
        public void GetSourceFormat_Unsupported_Http_With_Incorrect_MimeType()
        {
            var tiff = Path.GetFullPath(@"test_image.png");
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

                var mockLogger = new Mock<ILogger<ImageLoader>>();
                var mockCache = new Mock<IAppCache>();
                var loader = new ImageLoader(mockLogger.Object, mockCache.Object);
                var imageUri = new Uri("http://example.com/test_image.png");

                try
                {
                    var result = loader.GetSourceFormat(httpClient, imageUri, CancellationToken.None).Result;

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
