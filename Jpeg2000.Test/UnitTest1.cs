using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using C = TremendousIIIF.Common.Configuration;
using Image.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jpeg2000.Test
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class Jpeg2000Decoder
    {
        ILogger Log;
        [TestInitialize]
        public void Setup()
        {
            Log = new LoggerFactory().CreateLogger("test");
        }
        [TestMethod]
        public void RegionScale()
        {

            var filename = @"C:\JP2Cache\vdc_0000000388E8.0x000008";
            var request = new ImageRequest
            (
                new ImageRegion(ImageRegionMode.Region, 2048f, 0f, 9f, 1024f),
                new ImageSize(ImageSizeMode.Distort, 1, 3, 0),
                new ImageRotation(0, false),
                ImageQuality.@default,
                TremendousIIIF.Common.ImageFormat.jpg
            );
            var q = new C.ImageQuality();

            (_, var img) = J2KExpander.ExpandRegion(null, Log, new Uri(filename), request, false, q);
            using (img)
            {
                Assert.AreEqual(3, img.Width);
                Assert.AreEqual(342, img.Height);
            }
        }
    }
}
