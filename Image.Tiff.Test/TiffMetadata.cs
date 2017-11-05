using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Serilog;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Image.Tiff.Test
{
    [TestClass]
    [TestCategory("Tiff")]
    [TestCategory("Metadata")]
    [ExcludeFromCodeCoverage]
    public class TiffMetadata
    {

        ILogger Log;
        [TestInitialize]
        public void Setup()
        {
            Log = new LoggerConfiguration().CreateLogger();
        }

        [TestMethod]
        [Description("/test_image.tif/info.json")]
        public void Metadata()
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var defaultTileWidth = 512;
            var expectedWidth = 1000;
            var expectedHeight = 1000;

            var result = TiffExpander.GetMetadata(null, Log, new Uri(filename), defaultTileWidth, string.Empty);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTileWidth, result.TileWidth, "Returned TileWidth does not match expected value");
            Assert.AreEqual(0, result.TileHeight, "TileHeight should not be set");
            Assert.AreEqual(expectedWidth, result.Width, "Expected Width does not match returned value");
            Assert.AreEqual(expectedHeight, result.Height, "Expected Height does not match returned value");
            Assert.AreEqual(6, result.ScalingLevels, "Returned ScalingLevels does not match expected value");
        }

        [TestMethod]
        [Description("/my_imaginary_file.tif/info.json")]
        [ExpectedException(typeof(FileNotFoundException))]
        public void MetadataFileNotFound()
        {
            var filename = Path.GetFullPath(@"my_imaginary_file.tif");
            var defaultTileWidth = 512;

            var result = TiffExpander.GetMetadata(null, Log, new Uri(filename), defaultTileWidth, string.Empty);
        }

        [TestMethod]
        [Description("/my_imaginary_file.tif/info.json")]
        [ExpectedException(typeof(FileNotFoundException))]
        public void MetadataFileNotFoundHttp()
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });


            var httpClient = new HttpClient(handler.Object);

            var filename = @"http://example.com/my_imaginary_file.tif";
            var defaultTileWidth = 512;

            try
            {
                TiffExpander.GetMetadata(httpClient, Log, new Uri(filename), defaultTileWidth, string.Empty);
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        [Description("/my_imaginary_file.tif/info.json")]
        [ExpectedException(typeof(IOException))]
        public void MetadataHttpError()
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            


            var httpClient = new HttpClient(handler.Object);

            var filename = @"http://example.com/my_imaginary_file.tif";
            var defaultTileWidth = 512;

            try
            {
                TiffExpander.GetMetadata(httpClient, Log, new Uri(filename), defaultTileWidth, string.Empty);
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        [Description("/my_imaginary_file.tif/info.json")]
        [ExpectedException(typeof(TaskCanceledException))]
        public void MetadataHttpTaskCanceledException()
        {
            var handler = new Mock<MockHttpHandler>() { CallBase = true };
            handler
                .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                .Throws(new TaskCanceledException(Task.FromCanceled(new System.Threading.CancellationToken(true))));

            var httpClient = new HttpClient(handler.Object);

            var logger = new Mock<ILogger>();
            logger.Setup(l => l.Error(It.IsAny<Exception>(),It.IsAny<string>())).Verifiable();

            var filename = @"http://example.com/my_imaginary_file.tif";
            var defaultTileWidth = 512;

            try
            {
                TiffExpander.GetMetadata(httpClient, logger.Object, new Uri(filename), defaultTileWidth, string.Empty);
            }
            catch (AggregateException ex)
            {
                logger.Verify(v => v.Error(It.IsAny<TaskCanceledException>(), "HTTP Request Cancelled"), Times.Once);
                throw ex.InnerException;
            }
        }

        [TestMethod]
        [Description("/my_imaginary_file.tif/info.json")]
        public void MetadataFromHttp()
        {
            var tiff = Path.GetFullPath(@"test_image.tif");
            using (var fs = File.OpenRead(tiff))
            {
                var handler = new Mock<MockHttpHandler>() { CallBase = true };
                handler
                    .Setup(f => f.Send(It.IsAny<HttpRequestMessage>()))
                    .Returns(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StreamContent(fs)
                    });

                var httpClient = new HttpClient(handler.Object);

                var filename = @"http://example.com/my_imaginary_file.tif";
                var defaultTileWidth = 512;
                var expectedWidth = 1000;
                var expectedHeight = 1000;

                var result = TiffExpander.GetMetadata(httpClient, Log, new Uri(filename), defaultTileWidth, string.Empty);

                Assert.IsNotNull(result);
                Assert.AreEqual(defaultTileWidth, result.TileWidth, "Returned TileWidth does not match expected value");
                Assert.AreEqual(0, result.TileHeight, "TileHeight should not be set");
                Assert.AreEqual(expectedWidth, result.Width, "Expected Width does not match returned value");
                Assert.AreEqual(expectedHeight, result.Height, "Expected Height does not match returned value");
                Assert.AreEqual(6, result.ScalingLevels, "Returned ScalingLevels does not match expected value");
            }
        }
        
    }
}
