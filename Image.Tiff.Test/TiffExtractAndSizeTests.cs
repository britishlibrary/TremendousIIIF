using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Serilog;
using Image.Common;
using TremendousIIIF.Common;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using RGB = System.ValueTuple<byte, byte, byte>;

namespace Image.Tiff.Test
{
    [TestClass]
    [TestCategory("Tiff")]
    [ExcludeFromCodeCoverage]
    public class TiffExtractAndSizeTests
    {
        private ILogger Log;

        public TestContext TestContext { get; set; }

        #region Test regions and colours

        static RGB[,] TestColours = {
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

        [TestInitialize]
        public void Setup()
        {
            Log = new LoggerConfiguration().CreateLogger();
        }

        [TestMethod]
        [Description("/test_image.tif/full/full/0/default.jpg")]
        public void FullImage()
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.Full),
                new ImageSize(ImageSizeMode.Full),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, false);

            Assert.IsNotNull(img, "Image is null");
            Assert.AreEqual(1000, img.Width, "Image width does not match expected width");
            Assert.AreEqual(1000, img.Height, "Image height does not match expected height");
        }

        [TestMethod]
        [Description("/test_image.tif/x,y,w,h/full/0/default.jpg")]
        [DynamicData("TestRegions", DynamicDataSourceType.Method)]
        public void ExtractRegionFullSize(int x, int y, int width, int height)
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.Region, x, y, width, height),
                new ImageSize(ImageSizeMode.Full),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, false);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(width, img.Width, "Image width does not match expected width");
                Assert.AreEqual(height, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    (var coli, var colj) = (x / 100, y / 100);
                    var colour = TestColours[coli, colj];
                    foreach (var c in bmp.Pixels.Distinct())
                    {
                        Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                    }
                }
            }
        }

        [TestMethod]
        [Description("/test_image.tif/pct:x,y,w,h/full/0/default.jpg")]
        [DynamicData("TestRegionsPct", DynamicDataSourceType.Method)]
        public void ExtractPctRegionFullSize(float x, float y, float width, float height)
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.PercentageRegion, x, y, width, height),
                new ImageSize(ImageSizeMode.Full),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, false);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(width * 10, img.Width, "Image width does not match expected width");
                Assert.AreEqual(height * 10, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    (var coli, var colj) = (Convert.ToInt32(x / 10), Convert.ToInt32(y / 10));
                    var colour = TestColours[coli, colj];
                    foreach (var c in bmp.Pixels.Distinct())
                    {
                        Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                    }
                }
            }
        }

        [TestMethod]
        [Description("/test_image.tif/x,y,w,h/1000,1000/0/default.jpg")]
        [DynamicData("TestRegions", DynamicDataSourceType.Method)]
        public void ExtractRegionScaleUp(int x, int y, int width, int height)
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.Region, x, y, width, height),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 200, 200),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, true);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(200, img.Width, "Image width does not match expected width");
                Assert.AreEqual(200, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    (var coli, var colj) = (x / 100, y / 100);
                    var colour = TestColours[coli, colj];
                    foreach (var c in bmp.Pixels.Distinct())
                    {
                        Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                    }
                }
            }
        }

        [TestMethod]
        [Description("/test_image.tif/x,y,w,h/50,50/0/default.jpg")]
        [DynamicData("TestRegions", DynamicDataSourceType.Method)]
        public void ExtractRegionScaleDown(int x, int y, int width, int height)
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.Region, x, y, width, height),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 50, 50),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, true);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(50, img.Width, "Image width does not match expected width");
                Assert.AreEqual(50, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    (var coli, var colj) = (x / 100, y / 100);
                    var colour = TestColours[coli, colj];
                    foreach (var c in bmp.Pixels.Distinct())
                    {
                        Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                    }
                }
            }
        }
        [TestMethod]
        [Description("/test_image.tif/pct:x,y,w,h/50,50/0/default.jpg")]
        [DynamicData("TestRegionsPct", DynamicDataSourceType.Method)]
        public void ExtractPctRegionScaleDown(float x, float y, float width, float height)
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.PercentageRegion, x, y, width, height),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 50, 50),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, false);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(50, img.Width, "Image width does not match expected width");
                Assert.AreEqual(50, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    (var coli, var colj) = (Convert.ToInt32(x / 10), Convert.ToInt32(y / 10));
                    var colour = TestColours[coli, colj];
                    foreach (var c in bmp.Pixels.Distinct())
                    {
                        Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                    }
                }
            }
        }

        [TestMethod]
        [Description("/test_image.tif/pct:x,y,w,h/200,200/0/default.jpg")]
        [DynamicData("TestRegionsPct", DynamicDataSourceType.Method)]
        public void ExtractPctRegionScaleUp(float x, float y, float width, float height)
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.PercentageRegion, x, y, width, height),
                new ImageSize(ImageSizeMode.MaintainAspectRatio, 1, 200, 200),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, false);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(200, img.Width, "Image width does not match expected width");
                Assert.AreEqual(200, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    (var coli, var colj) = (Convert.ToInt32(x / 10), Convert.ToInt32(y / 10));
                    var colour = TestColours[coli, colj];
                    foreach (var c in bmp.Pixels.Distinct())
                    {
                        Assert.AreEqual(new SKColor(colour.Item1, colour.Item2, colour.Item3), c, "Expected colour values do not match");
                    }
                }
            }
        }

        [TestMethod]
        [Description("/test_image.tif/square/max/0/default.jpg")]
        public void ExtractSquareMax()
        {
            var filename = Path.GetFullPath(@"test_image.tif");
            var request = new ImageRequest
            (
                "",
                new ImageRegion(ImageRegionMode.Square),
                new ImageSize(ImageSizeMode.Max),
                new ImageRotation(0, false),
                ImageQuality.@default,
                ImageFormat.jpg
            );

            (var state, var img) = TiffExpander.ExpandRegion(null, Log, new Uri(filename), request, false);
            using (img)
            {
                Assert.IsNotNull(img, "Image is null");
                Assert.AreEqual(1000, img.Width, "Image width does not match expected width");
                Assert.AreEqual(1000, img.Height, "Image height does not match expected height");

                using (var bmp = SKBitmap.FromImage(img))
                {
                    Assert.AreEqual(100, bmp.Pixels.Distinct().Count());
                }
            }
        }

        [TestMethod]
        public void ExtractSquareScaleDown()
        {

        }

        [TestMethod]
        public void ExtractSquareScaleUp()
        {

        }
    }

}
