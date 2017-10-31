using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using C=TremendousIIIF.Common.Configuration;
using Serilog;
using Image.Common;
using System.Diagnostics.CodeAnalysis;

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
            Log = new LoggerConfiguration().CreateLogger();
        }
        [TestMethod]
        public void RegionScale()
        {

            var filename = @"C:\JP2Cache\vdc_0000000388E8.0x000008";
            var request = new ImageRequest
            {
                Format = TremendousIIIF.Common.ImageFormat.jpg,
                ID = "ark:/81055/vdc_0000000388E8.0x000008",
                MaxArea = 2147483647,
                MaxHeight = 2147483647,
                MaxWidth = 2147483647,
                Quality = ImageQuality.@default,
                Region =
                new ImageRegion
                {
                    Height = 1024f,
                    Mode = ImageRegionMode.Region,
                    Width = 9f,
                    X = 2048f,
                    Y = 0f
                },
                RequestId = null,
                Rotation = new ImageRotation
                {
                    Degrees = 0f,
                    Mirror = false
                },
                Size = new ImageSize
                {
                    Height = 0,
                    Mode = ImageSizeMode.Distort,
                    Percent = 1f,
                    Width = 3
                }
            };
            var q = new C.ImageQuality();

            (var state, var img) = J2KExpander.ExpandRegion(null, Log, new Uri(filename), request, false, q);
            using (img)
            {
                Assert.AreEqual(3, img.Width);
                Assert.AreEqual(342, img.Height);
            }
        }
    }
}
