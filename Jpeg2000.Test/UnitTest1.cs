using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using C = TremendousIIIF.Common.Configuration;
using Image.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using RGB = System.ValueTuple<byte, byte, byte>;
using TremendousIIIF.Common;
using SkiaSharp;
using System.Collections.Generic;
using static System.Math;

namespace Jpeg2000.Test
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class Jpeg2000Decoder
    {

        #region Test regions and colours

        static readonly RGB[,] TestColours = {
            { (61, 170, 126), (61, 107, 178), (82, 85, 234), (164, 122, 110), (129, 226, 88), (91, 37, 121), (138, 128, 42), (6, 85, 234), (121, 109, 204), (65, 246, 84) },
            { (195, 133, 120), (171, 43, 102), (118, 45, 130), (242, 105, 171), (5, 85, 105), (113, 58, 41), (223, 69, 3), (45, 79, 140), (35, 117, 248), (121, 156, 184) },
            { (168, 92, 163), (28, 91, 143), (86, 41, 173), (111, 230, 29), (174, 189, 7), (18, 139, 88), (93, 168, 128), (35, 2, 14), (204, 105, 137), (18, 86, 128) },
            { (107, 55, 178), (251, 40, 184), (47, 36, 139), (2, 127, 170), (224, 12, 114), (133, 67, 108), (239, 174, 209), (85, 29, 156), (8, 55, 188), (240, 125, 7) },
            { (112, 167, 30), (166, 63, 161), (232, 227, 23), (74, 80, 135), (79, 97, 47), (145, 160, 80), (45, 160, 79), (12, 54, 215), (203, 83, 70), (78, 28, 46) },
            { (102, 193, 63), (225, 55, 91), (107, 194, 147), (167, 24, 95), (249, 214, 96), (167, 34, 136), (53, 254, 209), (172, 222, 21), (153, 77, 51), (137, 39, 183) },
            { (159, 182, 192), (128, 252, 173), (148, 162, 90), (192, 165, 115), (154, 102, 2), (107, 237, 62), (111, 236, 219), (129, 113, 172), (239, 204, 166), (60, 96, 37) },
            { (72, 172, 227), (119, 51, 100), (209, 85, 165), (87, 172, 159), (188, 42, 162), (99, 3, 54), (7, 42, 37), (105, 155, 100), (38, 220, 240), (98, 46, 2) },
            { (18, 223, 145), (189, 121, 17), (88, 3, 210), (181, 16, 43), (189, 39, 244), (123, 147, 116), (246, 148, 214), (223, 177, 199), (77, 18, 136), (235, 36, 21) },
            { (146, 137, 176), (84, 248, 55), (61, 144, 79), (110, 251, 49), (43, 105, 132),(165, 131, 55), (60, 23, 225), (147, 197, 226), (80, 67, 104), (161, 119, 182) }
        };

        static IEnumerable<object[]> TestRegions()
        {
            return from x in Enumerable.Range(0, 10)
                   from y in Enumerable.Range(0, 10)
                   select new object[] { x * 100, y * 100, 100, 100 }.ToArray();
        }

        static IEnumerable<object[]> TestRegionsPct()
        {
            return from x in Enumerable.Range(0, 10)
                   from y in Enumerable.Range(0, 10)
                   select new object[] { x * 10f, y * 10f, 10f, 10f }.ToArray();
        }
        #endregion
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
                new ImageRegion(ImageRegionMode.Region, 2048f, 0f, 350f, 1024f),
                new ImageSize(ImageSizeMode.Full),
                new ImageRotation(0, false),
                ImageQuality.@default,
                TremendousIIIF.Common.ImageFormat.jpg
            );
            var q = new C.ImageQuality();
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 16, useAsync: true);

            (_, var img) = J2KExpander.ExpandRegion(fs, Log, new Uri(filename), request, false, q);
            using (img)
            {
                Assert.AreEqual(350, img.Width);
                Assert.AreEqual(1024, img.Height);
            }
        }

        [TestMethod]
        [Description("/test_image.jp2/x,y,w,h/full/0/default.jpg")]
        [DynamicData("TestRegions", DynamicDataSourceType.Method)]
        public void ExtractRegionFullSize(int x, int y, int width, int height)
        {
            var filename = Path.GetFullPath(@"testimage.jp2");
            var request = new ImageRequest
            (
                new ImageRegion(ImageRegionMode.Region, x, y, width, height),
                new ImageSize(ImageSizeMode.Full),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            var q = new C.ImageQuality { DefaultEncodingQuality = 100, MaxQualityLayers = -1, OutputDpi = 600, WeightedRMSE = 1.0f };
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 23, useAsync: true);
            (_, var img) = J2KExpander.ExpandRegion(fs, Log, new Uri(filename), request, false, q);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(width, img.Width, "Image width does not match expected width");
                Assert.AreEqual(height, img.Height, "Image height does not match expected height");

                using var bmp = SKBitmap.FromImage(img);
                (var coli, var colj) = (x / 100, y / 100);

                var truth = TestColours[coli, colj];
                var distinctPixels = bmp.Pixels.Distinct();

                foreach (var c in distinctPixels)
                {
                    //var ok = Math.Abs(colour.Item1 - distinctPixels.) < 6 && Math.Abs(colour.Item2-distinctPixels[1]) < 6 && Math.Abs(colour.Item3-distinctPixels[2]) < 6;
                    Assert.IsTrue(Abs(c.Red - truth.Item1) < 6, "Red value does nto match");
                    Assert.IsTrue(Abs(c.Green - truth.Item2) < 6, "Green value does nto match");
                    Assert.IsTrue(Abs(c.Blue - truth.Item3) < 6, "Blue value does nto match");
                    //Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                }
            }
        }
    }
}
