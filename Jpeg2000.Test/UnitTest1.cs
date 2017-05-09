using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TremendousIIIF.Common.Configuration;
using Serilog;

namespace Jpeg2000.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {

            var filename = @"C:\JP2Cache\vdc_0000000388E8.0x000008";
            var request = new Image.Common.ImageRequest
            {
                Format = TremendousIIIF.Common.ImageFormat.jpg,
                ID = "ark:/81055/vdc_0000000388E8.0x000008",
                MaxArea = 2147483647,
                MaxHeight = 2147483647,
                MaxWidth = 2147483647,
                Quality = Image.Common.ImageQuality.@default,
                Region =
                new Image.Common.ImageRegion
                {
                    Height = 1024f,
                    Mode = Image.Common.ImageRegionMode.Region,
                    Width = 9f,
                    X = 2048f,
                    Y = 0f
                },
                RequestId = null,
                Rotation = new Image.Common.ImageRotation
                {
                    Degrees = 0f,
                    Mirror = false
                },
                Size = new Image.Common.ImageSize
                {
                    Height = 0,
                    Mode = Image.Common.ImageSizeMode.Exact,
                    Percent = 1f,
                    Width = 3
                }
            };
            var q = new ImageQuality();
            var log = new LoggerConfiguration().CreateLogger();

            (var state, var img) = J2KExpander.ExpandRegion(null, log, new Uri(filename), request, false, q);
            using (img)
            {
                Assert.AreEqual(3, img.Width);
                Assert.AreEqual(342, img.Height);
            }
        }
    }
}
