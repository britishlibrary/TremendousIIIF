using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
            Log = new LoggerFactory().CreateLogger("test");
        }

        [TestMethod]
        [Description("/test_image.tif/info.json")]
        public void Metadata()
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var defaultTileWidth = 512;
            var expectedWidth = 1000;
            var expectedHeight = 1000;

            var result = TiffExpander.GetMetadata(null, Log, new Uri(filename), defaultTileWidth);

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

            var result =  TiffExpander.GetMetadata(null, Log, new Uri(filename), defaultTileWidth);
        }
               
    }
}
